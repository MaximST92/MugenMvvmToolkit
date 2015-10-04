﻿#region Copyright

// ****************************************************************************
// <copyright file="MvvmApplication.cs">
// Copyright (c) 2012-2015 Vyacheslav Volkov
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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using MugenMvvmToolkit.Infrastructure;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Models;

namespace MugenMvvmToolkit
{
    public abstract class MvvmApplication : IMvvmApplication
    {
        #region Fields

        private const int InitializedState = 1;
        private int _state;

        private readonly LoadMode _mode;
        private PlatformInfo _platform;
        private IViewModelSettings _viewModelSettings;
        private IIocContainer _iocContainer;
        private readonly IDataContext _context;

        #endregion

        #region Constructors

        protected MvvmApplication(LoadMode mode = LoadMode.Runtime)
        {
            ServiceProvider.DesignTimeManager = DesignTimeManagerImpl.Instance;
            Current = this;
            _mode = mode;
            _platform = PlatformInfo.Unknown;
            _context = new DataContext();
        }

        #endregion

        #region Properties

        public static IMvvmApplication Current { get; protected set; }

        public virtual bool IsInitialized
        {
            get { return _state == InitializedState; }
        }

        public virtual PlatformInfo Platform
        {
            get { return _platform; }
        }

        public virtual LoadMode Mode
        {
            get { return _mode; }
        }

        public virtual IIocContainer IocContainer
        {
            get { return _iocContainer; }
        }

        public virtual IDataContext Context
        {
            get { return _context; }
        }

        public virtual IViewModelSettings ViewModelSettings
        {
            get { return _viewModelSettings; }
        }

        #endregion

        #region Methods

        public static event EventHandler Initialized;

        public static void InitializeDesignTimeManager()
        {
            // ReSharper disable once UnusedVariable
            var dummy = ServiceProvider.DesignTimeManager;
        }

        public static void SetDefaultDesignTimeManager()
        {
            ServiceProvider.DesignTimeManager = DesignTimeManagerImpl.Instance;
        }

        protected virtual void OnInitialize(IList<Assembly> assemblies)
        {
            LoadModules(assemblies);
            if (!IocContainer.TryGet(out _viewModelSettings))
            {
                _viewModelSettings = CreateViewModelSettings();
                IocContainer.BindToConstant(_viewModelSettings);
            }
        }

        protected virtual void LoadModules(IList<Assembly> assemblies)
        {
            var loadedModules = GetModules(assemblies);
            if (loadedModules != null && loadedModules.Count != 0)
            {
                var context = CreateModuleContext(assemblies);
                context.LoadModules(loadedModules);
            }
        }

        [NotNull]
        protected virtual IViewModelSettings CreateViewModelSettings()
        {
            return new DefaultViewModelSettings();
        }

        [NotNull]
        protected virtual IModuleContext CreateModuleContext(IList<Assembly> assemblies)
        {
            return new ModuleContext(Platform, Mode, IocContainer, Context, assemblies);
        }

        protected virtual IList<IModule> GetModules(IList<Assembly> assemblies)
        {
            if (Mode == LoadMode.Design)
                return Empty.Array<IModule>();
            return assemblies.GetModules(true);
        }

        protected static void RaiseInitialized(IMvvmApplication sender)
        {
            var handler = Initialized;
            if (handler != null) handler(sender, EventArgs.Empty);
        }

        #endregion

        #region Implementation of interfaces

        public void Initialize(PlatformInfo platform, IIocContainer iocContainer, IList<Assembly> assemblies,
            IDataContext context)
        {
            Should.NotBeNull(platform, "platform");
            Should.NotBeNull(iocContainer, "iocContainer");
            Should.NotBeNull(assemblies, "assemblies");
            if (Interlocked.Exchange(ref _state, InitializedState) == InitializedState)
                return;
            Current = this;
            _platform = platform;
            _iocContainer = iocContainer;
            if (context != null)
                Context.Merge(context);
            OnInitialize(assemblies);
            RaiseInitialized(this);
        }

        public abstract Type GetStartViewModelType();

        #endregion
    }
}