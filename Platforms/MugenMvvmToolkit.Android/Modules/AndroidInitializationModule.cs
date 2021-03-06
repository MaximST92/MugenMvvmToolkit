﻿#region Copyright

// ****************************************************************************
// <copyright file="AndroidInitializationModule.cs">
// Copyright (c) 2012-2017 Vyacheslav Volkov
// </copyright>
// ****************************************************************************
// <author>Vyacheslav Volkov</author>
// <email>vvs0205@outlook.com</email>
// <project>MugenMvvmToolkit</project>
// <web>https://github.com/MugenMvvmToolkit/MugenMvvmToolkit</web>
// <license>
// See license.txt in this solution or http://opensource.org/licenses/MS-PL
// </license>
// ****************************************************************************

#endregion

using Android.App;
using MugenMvvmToolkit.Android.Binding.Infrastructure;
using MugenMvvmToolkit.Android.Infrastructure;
using MugenMvvmToolkit.Android.Infrastructure.Callbacks;
using MugenMvvmToolkit.Android.Infrastructure.Navigation;
using MugenMvvmToolkit.Android.Infrastructure.Presenters;
using MugenMvvmToolkit.Android.Interfaces;
using MugenMvvmToolkit.Android.Interfaces.Navigation;
using MugenMvvmToolkit.Infrastructure.Presenters;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Callbacks;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.Navigation;
using MugenMvvmToolkit.Interfaces.Presenters;
using MugenMvvmToolkit.Models.IoC;
using MugenMvvmToolkit.Modules;

namespace MugenMvvmToolkit.Android.Modules
{
    public class AndroidInitializationModule : InitializationModuleBase
    {
        #region Methods

        public override bool Load(IModuleContext context)
        {
            var mediatorFactory = AndroidToolkitExtensions.MediatorFactory;
            AndroidToolkitExtensions.MediatorFactory = (o, dataContext, arg3) =>
            {
                return AndroidToolkitExtensions.MvvmActivityMediatorDefaultFactory(o, dataContext, arg3) ?? mediatorFactory?.Invoke(o, dataContext, arg3);
            };
            AndroidToolkitExtensions.LayoutInflaterFactory = (c, dataContext, factory, inflater) =>
            {
                if (inflater == null)
                {
                    Tracer.Error("The bindable inflater cannot be created without the original inflater");
                    return null;
                }
                LayoutInflaterFactoryWrapper.SetFactory(inflater, factory);
                return inflater;
            };
            AndroidToolkitExtensions.MenuInflaterFactory = (c, baseMenuInflater, ctx) => new BindableMenuInflater(c) { NestedMenuInflater = baseMenuInflater };
            AndroidToolkitExtensions.GetContentView = AndroidToolkitExtensions.GetContentViewDefault;
            AndroidToolkitExtensions.AddContentViewManager(new ViewContentViewManager());

            BindNavigationService(context, context.IocContainer);
            BindViewFactory(context, context.IocContainer);

            return base.Load(context);
        }

        protected virtual void BindNavigationService(IModuleContext context, IIocContainer container)
        {
            container.Bind<INavigationService, NavigationService>(DependencyLifecycle.SingleInstance);
        }

        protected virtual void BindViewFactory(IModuleContext context, IIocContainer container)
        {
            container.Bind<IViewFactory, ViewFactory>(DependencyLifecycle.SingleInstance);
        }

        protected override void BindReflectionManager(IModuleContext context, IIocContainer container)
        {
            IReflectionManager reflectionManager = new ExpressionReflectionManagerEx();
            ToolkitServiceProvider.ReflectionManager = reflectionManager;
            container.BindToConstant(reflectionManager);
        }

        protected override void BindViewModelPresenter(IModuleContext context, IIocContainer container)
        {
            container.BindToMethod((iocContainer, list) =>
            {
                IViewModelPresenter presenter = iocContainer.Get<ViewModelPresenter>();
                presenter.DynamicPresenters.Add(new DynamicViewModelNavigationPresenter());
                return presenter;
            }, DependencyLifecycle.SingleInstance);
        }

        protected override void BindMessagePresenter(IModuleContext context, IIocContainer container)
        {
            container.Bind<IMessagePresenter, MessagePresenter>(DependencyLifecycle.SingleInstance);
        }

        protected override void BindToastPresenter(IModuleContext context, IIocContainer container)
        {
            container.Bind<IToastPresenter, ToastPresenter>(DependencyLifecycle.SingleInstance);
        }

        protected override void BindTracer(IModuleContext context, IIocContainer container)
        {
            ITracer tracer = new TracerEx();
            ToolkitServiceProvider.Tracer = tracer;
            container.BindToConstant(tracer);
        }

        protected override void BindThreadManager(IModuleContext context, IIocContainer container)
        {
            ToolkitServiceProvider.ThreadManager = new ThreadManager(Application.SynchronizationContext);
            container.BindToConstant(ToolkitServiceProvider.ThreadManager);
        }

        protected override void BindNavigationProvider(IModuleContext context, IIocContainer container)
        {
            container.Bind<INavigationProvider, NavigationProvider>(DependencyLifecycle.SingleInstance);
        }

        protected override void BindOperationCallbackFactory(IModuleContext context, IIocContainer container)
        {
            container.Bind<IOperationCallbackFactory, SerializableOperationCallbackFactory>(DependencyLifecycle.SingleInstance);
        }

        protected override void BindAttachedValueProvider(IModuleContext context, IIocContainer container)
        {
            IAttachedValueProvider attachedValueProvider = new AttachedValueProvider();
            ToolkitServiceProvider.AttachedValueProvider = attachedValueProvider;
            container.BindToConstant(attachedValueProvider);
        }

        #endregion
    }
}