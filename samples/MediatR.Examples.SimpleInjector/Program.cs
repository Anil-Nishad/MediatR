using System.IO;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace MediatR.Examples.SimpleInjector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::SimpleInjector;
#if NETCOREAPP3_1
    using MediatR.Pipeline.Streams;
#endif

    internal static class Program
    {
        private static Task Main(string[] args)
        {
            var writer = new WrappingWriter(Console.Out);
            var mediator = BuildMediator(writer);

            return Runner.Run(mediator, writer, "SimpleInjector", true);
        }

        private static IMediator BuildMediator(WrappingWriter writer)
        {
            var container = new Container();
            var assemblies = GetAssemblies().ToArray();
            container.RegisterSingleton<IMediator, Mediator>();
            container.Register(typeof(IRequestHandler<,>), assemblies);

            RegisterHandlers(container, typeof(INotificationHandler<>), assemblies);
            RegisterHandlers(container, typeof(IRequestExceptionAction<,>), assemblies);
            RegisterHandlers(container, typeof(IRequestExceptionHandler<,,>), assemblies);

#if NETCOREAPP3_1
            container.Register(typeof(IStreamRequestHandler<,>), assemblies);

            RegisterHandlers(container, typeof(IStreamRequestExceptionAction<,>), assemblies);
            RegisterHandlers(container, typeof(IStreamRequestExceptionHandler<,,>), assemblies);
#endif

            container.Register(() => (TextWriter)writer, Lifestyle.Singleton);

            //Pipeline
            container.Collection.Register(typeof(IPipelineBehavior<,>), new []
            {
                typeof(RequestExceptionProcessorBehavior<,>),
                typeof(RequestExceptionActionProcessorBehavior<,>),
                typeof(RequestPreProcessorBehavior<,>),
                typeof(RequestPostProcessorBehavior<,>),
                typeof(GenericPipelineBehavior<,>)
            });
            container.Collection.Register(typeof(IRequestPreProcessor<>), new [] { typeof(GenericRequestPreProcessor<>) });
            container.Collection.Register(typeof(IRequestPostProcessor<,>), new[] { typeof(GenericRequestPostProcessor<,>), typeof(ConstrainedRequestPostProcessor<,>) });


#if NETCOREAPP3_1
            //Pipeline.Streams
            container.Collection.Register(typeof(IStreamPipelineBehavior<,>), new[]
            {
                typeof(StreamRequestExceptionProcessorBehavior<,>),
                typeof(StreamRequestExceptionActionProcessorBehavior<,>),
                typeof(StreamRequestPreProcessorBehavior<,>),
                typeof(StreamRequestPostProcessorBehavior<,>),
                typeof(GenericStreamPipelineBehavior<,>)
            });
            container.Collection.Register(typeof(IStreamRequestPreProcessor<>), new[] { typeof(GenericStreamRequestPreProcessor<>) });
            container.Collection.Register(typeof(IStreamRequestPostProcessor<,>), new[] { typeof(GenericStreamRequestPostProcessor<,>), typeof(ConstrainedStreamRequestPostProcessor<,>) });
#endif

            container.Register(() => new ServiceFactory(container.GetInstance), Lifestyle.Singleton);

            container.Verify();

            var mediator = container.GetInstance<IMediator>();

            return mediator;
        }

        private static void RegisterHandlers(Container container, Type collectionType, Assembly[] assemblies)
        {
            // we have to do this because by default, generic type definitions (such as the Constrained Notification Handler) won't be registered
            var handlerTypes = container.GetTypesToRegister(collectionType, assemblies, new TypesToRegisterOptions
            {
                IncludeGenericTypeDefinitions = true,
                IncludeComposites = false,
            });

            container.Collection.Register(collectionType, handlerTypes);
        }

        private static IEnumerable<Assembly> GetAssemblies()
        {
            yield return typeof(IMediator).GetTypeInfo().Assembly;
            yield return typeof(Ping).GetTypeInfo().Assembly;
        }
    }
}
