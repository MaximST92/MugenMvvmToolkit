﻿#region Copyright

// ****************************************************************************
// <copyright file="NavigationService.cs">
// Copyright (c) 2012-2016 Vyacheslav Volkov
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

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using MugenMvvmToolkit.Android.Binding;
using MugenMvvmToolkit.Android.Infrastructure.Mediators;
using MugenMvvmToolkit.Android.Interfaces.Navigation;
using MugenMvvmToolkit.Android.Interfaces.Views;
using MugenMvvmToolkit.Android.Models.EventArg;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.DataConstants;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit.Models.EventArg;
using Object = Java.Lang.Object;

namespace MugenMvvmToolkit.Android.Infrastructure.Navigation
{
    public class NavigationService : INavigationService
    {
        #region Nested Types

        private sealed class ActivityLifecycleListener : Object, Application.IActivityLifecycleCallbacks
        {
            #region Fields

            private readonly NavigationService _service;

            #endregion

            #region Constructors

            public ActivityLifecycleListener(NavigationService service)
            {
                _service = service;
            }

            public ActivityLifecycleListener(IntPtr handle, JniHandleOwnership transfer)
                : base(handle, transfer)
            {
            }

            #endregion

            #region Methods

            private bool IsAlive()
            {
                if (_service == null)
                {
                    (Application.Context as Application)?.UnregisterActivityLifecycleCallbacks(this);
                    return false;
                }
                return true;
            }

            private void RaiseNew(Activity activity)
            {
                if (IsAlive() && !(activity is IActivityView) && _service.CurrentContent != activity)
                {
                    PlatformExtensions.SetCurrentActivity(activity, false);
                    _service.RaiseNavigated(activity, NavigationMode.New, null, null);
                }
            }

            #endregion

            #region Implementation of interfaces

            public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
            {
                RaiseNew(activity);
            }

            public void OnActivityDestroyed(Activity activity)
            {
                if (IsAlive() && !(activity is IActivityView))
                    PlatformExtensions.SetCurrentActivity(activity, true);
            }

            public void OnActivityPaused(Activity activity)
            {
            }

            public void OnActivityResumed(Activity activity)
            {
            }

            public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
            {
            }

            public void OnActivityStarted(Activity activity)
            {
                RaiseNew(activity);
            }

            public void OnActivityStopped(Activity activity)
            {
            }

            #endregion
        }

        #endregion

        #region Fields

        private const string ParameterString = "viewmodelparameter";

        private bool _isBack;
        private bool _isNew;
        private bool _isReorder;
        private bool _isPause;
        private string _parameter;
        private IDataContext _dataContext;

        #endregion

        #region Constructors

        [Preserve(Conditional = true)]
        public NavigationService()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich)
            {
                (Application.Context as Application)?.RegisterActivityLifecycleCallbacks(new ActivityLifecycleListener(this));
            }
        }

        #endregion

        #region Methods

        protected virtual bool RaiseNavigating(NavigatingCancelEventArgs args)
        {
            var handler = Navigating;
            if (handler != null)
            {
                handler(this, args);
                return !args.Cancel;
            }
            return true;
        }

        protected virtual void RaiseNavigated(object content, NavigationMode mode, string parameter, IDataContext context)
        {
            Navigated?.Invoke(this, new NavigationEventArgs(content, parameter, mode, context));
        }

        private static string GetParameterFromIntent(Intent intent)
        {
            return intent?.GetStringExtra(ParameterString);
        }

        protected virtual void StartActivity(Context context, Intent intent, IViewMappingItem source, IDataContext dataContext)
        {
            var activity = context.GetActivity();
            Action<Context, Intent, IViewMappingItem, IDataContext> startAction = null;
            if (activity != null)
                startAction = activity.GetBindingMemberValue(AttachedMembers.Activity.StartActivityDelegate);
            if (startAction == null)
                context.StartActivity(intent);
            else
                startAction(context, intent, source, dataContext);
        }

        protected virtual void GoBack()
        {
            var currentActivity = PlatformExtensions.CurrentActivity;
            if (currentActivity != null && !currentActivity.IsFinishing)
                currentActivity.OnBackPressed();
        }

        private static IDataContext MergeContext(IDataContext ctx1, IDataContext ctx2)
        {
            if (ctx1 == null && ctx2 == null)
                return null;
            if (ctx1 != null && ctx2 != null)
            {
                ctx1 = ctx1.ToNonReadOnly();
                ctx1.Merge(ctx2);
                return ctx1;
            }
            if (ctx1 != null)
                return ctx1;
            return ctx2;
        }

        #endregion

        #region Implementation of INavigationService

        public virtual object CurrentContent => PlatformExtensions.CurrentActivity;

        public virtual void OnPauseActivity(Activity activity, IDataContext context = null)
        {
            Should.NotBeNull(activity, nameof(activity));
            if (_isNew || _isBack || !ReferenceEquals(activity, CurrentContent))
                return;
            _isPause = true;
            RaiseNavigating(NavigatingCancelEventArgs.NonCancelableEventArgs);
            RaiseNavigated(null, NavigationMode.New, null, context);
        }

        public virtual void OnResumeActivity(Activity activity, IDataContext context = null)
        {
            Should.NotBeNull(activity, nameof(activity));
            if (ReferenceEquals(activity, CurrentContent) && !_isPause)
                return;
            PlatformExtensions.SetCurrentActivity(activity, false);
            _isPause = false;
            if (_isNew)
            {
                var isReorder = _isReorder;
                _isNew = false;
                _isReorder = false;
                RaiseNavigated(activity, isReorder ? NavigationMode.Refresh : NavigationMode.New, _parameter, MergeContext(_dataContext, context));
                _parameter = null;
                _dataContext = null;
            }
            else
            {
                _isBack = false;
                RaiseNavigated(activity, NavigationMode.Back, GetParameterFromIntent(activity.Intent), MergeContext(_dataContext, context));
            }
        }

        public virtual void OnStartActivity(Activity activity, IDataContext context = null)
        {
            OnResumeActivity(activity);
        }

        public virtual void OnCreateActivity(Activity activity, IDataContext context = null)
        {
            OnResumeActivity(activity);
        }

        public virtual bool OnFinishActivity(Activity activity, bool isBackNavigation, IDataContext context = null)
        {
            Should.NotBeNull(activity, nameof(activity));
            if (!isBackNavigation)
                return true;
            if (!RaiseNavigating(new NavigatingCancelEventArgs(NavigationMode.Back, MergeContext(_dataContext, context))))
                return false;
            //If it's the first activity, we need to raise the back navigation event.
            if (activity.IsTaskRoot)
                RaiseNavigated(null, NavigationMode.Back, null, MergeContext(_dataContext, context));
            _isBack = true;
            return true;
        }

        public virtual string GetParameterFromArgs(EventArgs args)
        {
            Should.NotBeNull(args, nameof(args));
            var cancelArgs = args as NavigatingCancelEventArgs;
            if (cancelArgs == null)
                return (args as NavigationEventArgs)?.Parameter;
            return cancelArgs.Parameter;
        }

        public virtual bool Navigate(NavigatingCancelEventArgsBase args)
        {
            Should.NotBeNull(args, nameof(args));
            if (!args.IsCancelable)
                return false;
            var eventArgs = (NavigatingCancelEventArgs)args;

            if (args.Context != null && eventArgs.NavigationMode == NavigationMode.Remove)
                return TryClose(args.Context);

            if (eventArgs.NavigationMode != NavigationMode.Back && eventArgs.Mapping != null)
                return Navigate(eventArgs.Mapping, eventArgs.Parameter, eventArgs.Context);

            var activity = PlatformExtensions.CurrentActivity;
            if (activity == null)
                return false;
            GoBack();
            return activity.IsFinishing;
        }

        public virtual bool Navigate(IViewMappingItem source, string parameter, IDataContext dataContext)
        {
            Should.NotBeNull(source, nameof(source));
            if (dataContext == null)
                dataContext = DataContext.Empty;
            bool bringToFront;
            dataContext.TryGetData(NavigationProviderConstants.BringToFront, out bringToFront);
            if (!RaiseNavigating(new NavigatingCancelEventArgs(source, bringToFront ? NavigationMode.Refresh : NavigationMode.New, parameter, dataContext)))
                return false;
            var activity = PlatformExtensions.CurrentActivity;
            var context = activity ?? Application.Context;

            var intent = new Intent(context, source.ViewType);
            if (activity == null)
                intent.AddFlags(ActivityFlags.NewTask);
            else if (dataContext.GetData(NavigationConstants.ClearBackStack))
            {
                if (PlatformExtensions.IsApiLessThanOrEqualTo10)
                    intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                else
                    intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                ServiceProvider.EventAggregator.Publish(this, MvvmActivityMediator.FinishActivityMessage.Instance);
                dataContext.AddOrUpdate(NavigationProviderConstants.InvalidateAllCache, true);
            }
            if (parameter != null)
                intent.PutExtra(ParameterString, parameter);

            if (bringToFront)
            {
                _isReorder = true;
                //http://stackoverflow.com/questions/20695522/puzzling-behavior-with-reorder-to-front
                //http://code.google.com/p/android/issues/detail?id=63570#c2
                bool closed = false;
                if (PlatformExtensions.IsApiGreaterThanOrEqualTo19)
                {
                    var viewModel = dataContext.GetData(NavigationConstants.ViewModel);
                    if (viewModel != null)
                    {
                        var view = viewModel.Settings.Metadata.GetData(ViewModelConstants.View);
                        var activityView = ToolkitExtensions.GetUnderlyingView<object>(view) as Activity;
                        if (activityView != null && activityView.IsTaskRoot)
                        {
                            var message = new MvvmActivityMediator.FinishActivityMessage(viewModel);
                            ServiceProvider.EventAggregator.Publish(this, message);
                            closed = message.Finished;
                        }
                    }
                }
                if (!closed)
                    intent.AddFlags(ActivityFlags.ReorderToFront);
                dataContext.AddOrUpdate(NavigationProviderConstants.InvalidateCache, true);
            }
            _isNew = true;
            _parameter = parameter;
            _dataContext = dataContext;
            StartActivity(context, intent, source, dataContext);
            return true;
        }

        public virtual bool CanClose(IDataContext dataContext)
        {
            Should.NotBeNull(dataContext, nameof(dataContext));
            var viewModel = dataContext.GetData(NavigationConstants.ViewModel);
            if (viewModel == null)
                return false;
            if (CurrentContent?.DataContext() == viewModel)
                return true;
            var message = new MvvmActivityMediator.CanFinishActivityMessage(viewModel);
            ServiceProvider.EventAggregator.Publish(this, message);
            return message.CanFinish;
        }

        public virtual bool TryClose(IDataContext dataContext)
        {
            Should.NotBeNull(dataContext, nameof(dataContext));
            var viewModel = dataContext.GetData(NavigationConstants.ViewModel);
            if (viewModel == null)
                return false;
            if (CurrentContent?.DataContext() == viewModel)
            {
                _dataContext = dataContext;
                GoBack();
                return true;
            }

            if (!CanClose(dataContext) || !RaiseNavigating(new NavigatingCancelEventArgs(NavigationMode.Remove, dataContext)))
                return false;

            var message = new MvvmActivityMediator.FinishActivityMessage(viewModel);
            ServiceProvider.EventAggregator.Publish(this, message);
            if (message.Finished)
                RaiseNavigated(viewModel, NavigationMode.Remove, null, dataContext);
            return message.Finished;
        }

        public virtual event EventHandler<INavigationService, NavigatingCancelEventArgsBase> Navigating;

        public virtual event EventHandler<INavigationService, NavigationEventArgsBase> Navigated;

        #endregion
    }
}
