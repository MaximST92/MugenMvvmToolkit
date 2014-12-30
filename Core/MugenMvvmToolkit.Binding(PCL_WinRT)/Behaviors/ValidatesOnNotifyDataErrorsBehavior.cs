﻿#region Copyright

// ****************************************************************************
// <copyright file="ValidatesOnNotifyDataErrorsBehavior.cs">
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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MugenMvvmToolkit.Binding.Interfaces;
using MugenMvvmToolkit.Binding.Interfaces.Accessors;
using MugenMvvmToolkit.Binding.Interfaces.Models;
using MugenMvvmToolkit.Binding.Interfaces.Sources;
using MugenMvvmToolkit.Binding.Models.EventArg;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.Validation;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit.Models.EventArg;

namespace MugenMvvmToolkit.Binding.Behaviors
{
    /// <summary>
    ///     Represents the binding behavior that checks for errors that are raised by a data source that implements <see cref="INotifyDataErrorInfo" />.
    /// </summary>
    public class ValidatesOnNotifyDataErrorsBehavior : BindingBehaviorBase, IEventListener, IHasWeakReference
    {
        #region Fields

        private const string Key = "@$be.";

        /// <summary>
        ///     Gets the id of behavior.
        /// </summary>
        public static readonly Guid IdNotifyDataErrorInfoBindingBehavior;
        internal static readonly ValidatesOnNotifyDataErrorsBehavior Prototype;
        private static readonly EventInfo ErrorsChangedEvent;

        private List<IDisposable> _subscribers;
        private WeakReference _selfReference;
        private string _senderKey;

        #endregion

        #region Constructors

        static ValidatesOnNotifyDataErrorsBehavior()
        {
            ErrorsChangedEvent = typeof(INotifyDataErrorInfo).GetEventEx("ErrorsChanged", MemberFlags.Instance | MemberFlags.Public);
            IdNotifyDataErrorInfoBindingBehavior = new Guid("198CBAA2-CF75-4620-9BDD-A1EBF9B8B2F4");
            Prototype = new ValidatesOnNotifyDataErrorsBehavior();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the error paths.
        /// </summary>
        public string[] ErrorPaths { get; set; }

        #endregion

        #region Overrides of BindingBehaviorBase

        /// <summary>
        ///     Gets the id of behavior. Each <see cref="IDataBinding" /> can have only one instance with the same id.
        /// </summary>
        public override Guid Id
        {
            get { return IdNotifyDataErrorInfoBindingBehavior; }
        }

        /// <summary>
        ///     Gets the behavior priority.
        /// </summary>
        public override int Priority
        {
            get { return 0; }
        }

        /// <summary>
        ///     Attaches to the specified binding.
        /// </summary>
        protected override bool OnAttached()
        {
            if (!CanAttach())
                return false;
            _senderKey = Key + Binding.TargetAccessor.Source.Path.Path;
            EventHandler<IBindingSource, ValueChangedEventArgs> handler = OnBindingSourceValueChanged;
            var accessor = Binding.SourceAccessor as ISingleBindingSourceAccessor;
            if (_subscribers == null)
            {
                _subscribers = new List<IDisposable>(accessor == null ? 1 : Binding.SourceAccessor.Sources.Count);
                _selfReference = ServiceProvider.WeakReferenceFactory(this, true);
            }

            if (accessor == null)
            {
                var sources = Binding.SourceAccessor.Sources;
                for (int index = 0; index < sources.Count; index++)
                    sources[index].ValueChanged += handler;
            }
            else
                accessor.Source.ValueChanged += handler;
            UpdateSources(false);
            UpdateErrors();
            return true;
        }

        /// <summary>
        ///     Detaches this instance from its associated binding.
        /// </summary>
        protected override void OnDetached()
        {
            EventHandler<IBindingSource, ValueChangedEventArgs> handler = OnBindingSourceValueChanged;
            var accessor = Binding.SourceAccessor as ISingleBindingSourceAccessor;
            if (accessor == null)
            {
                var sources = Binding.SourceAccessor.Sources;
                for (int index = 0; index < sources.Count; index++)
                    sources[index].ValueChanged -= handler;
            }
            else
                accessor.Source.ValueChanged -= handler;
            UpdateSources(true);
            lock (_subscribers)
            {
                // Ensure that all concurrent adds have completed. 
            }
            UpdateErrors(Empty.Array<object>());
        }

        /// <summary>
        ///     Creates a new binding behavior that is a copy of the current instance.
        /// </summary>
        protected override IBindingBehavior CloneInternal()
        {
            var errorPaths = ErrorPaths;
            var behavior = new ValidatesOnNotifyDataErrorsBehavior();
            if (errorPaths != null)
                behavior.ErrorPaths = errorPaths.ToArrayEx();
            return behavior;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Defines the method that determines whether the behavior can attach to binding.
        /// </summary>
        protected virtual bool CanAttach()
        {
            return BindingServiceProvider.ErrorProvider != null;
        }

        /// <summary>
        /// Updates the current errors.
        /// </summary>
        protected virtual void UpdateErrors([CanBeNull] IList<object> errors)
        {
            var errorProvider = BindingServiceProvider.ErrorProvider;
            var binding = Binding;
            if (errorProvider == null || binding == null)
                return;
            var target = binding.TargetAccessor.Source.GetPathMembers(false).PenultimateValue;
            if (target != null && !target.IsUnsetValue())
                errorProvider.SetErrors(target, _senderKey, errors ?? Empty.Array<object>(), binding.Context);
        }

        private void UpdateSources(bool detach)
        {
            lock (_subscribers)
            {
                for (int i = 0; i < _subscribers.Count; i++)
                    _subscribers[i].Dispose();
                _subscribers.Clear();
                if (detach)
                    return;
                var accessor = Binding.SourceAccessor as ISingleBindingSourceAccessor;
                if (accessor == null)
                {
                    var sources = Binding.SourceAccessor.Sources;
                    for (int index = 0; index < sources.Count; index++)
                        TrySubscribe(sources[index]);
                }
                else
                    TrySubscribe(accessor.Source);
            }
        }

        private void UpdateErrors()
        {
            List<object> errors = null;
            lock (_subscribers)
            {
                var accessor = Binding.SourceAccessor as ISingleBindingSourceAccessor;
                if (accessor == null)
                {
                    var sources = Binding.SourceAccessor.Sources;
                    for (int index = 0; index < sources.Count; index++)
                        CollectErrors(ref errors, sources[index]);
                }
                else
                    CollectErrors(ref errors, accessor.Source);
            }
            UpdateErrors(errors);
        }

        private void TrySubscribe(IBindingSource source)
        {
            var dataErrorInfo = source.GetPathMembers(false).PenultimateValue as INotifyDataErrorInfo;
            if (dataErrorInfo == null)
                return;
            var subscriber = BindingServiceProvider.WeakEventManager.TrySubscribe(dataErrorInfo, ErrorsChangedEvent, this);
            if (subscriber != null)
                _subscribers.Add(subscriber);
        }

        private void OnBindingSourceValueChanged(IBindingSource sender, ValueChangedEventArgs args)
        {
            if (args.LastMemberChanged)
                return;
            UpdateSources(false);
            UpdateErrors();
        }

        private void CollectErrors(ref List<object> errors, IBindingSource bindingSource)
        {
            var notifyDataErrorInfo = bindingSource.GetPathMembers(false).PenultimateValue as INotifyDataErrorInfo;
            if (notifyDataErrorInfo == null)
                return;
            var path = bindingSource.Path.Parts.LastOrDefault();
            var paths = ErrorPaths;
            if (!string.IsNullOrEmpty(path) || paths == null || paths.Length == 0)
                CollectErrors(notifyDataErrorInfo, path, ref errors);
            if (paths != null)
            {
                for (int i = 0; i < paths.Length; i++)
                    CollectErrors(notifyDataErrorInfo, paths[i], ref errors);
            }
        }

        private static void CollectErrors(INotifyDataErrorInfo notifyDataErrorInfo, string path, ref List<object> errors)
        {
            var values = notifyDataErrorInfo.GetErrors(path ?? string.Empty);
            if (values == null)
                return;
            foreach (var error in values)
            {
                if (error == null)
                    continue;
                if (errors == null)
                    errors = new List<object>();
                errors.Add(error);
            }
        }

        private bool Handle(DataErrorsChangedEventArgs message)
        {
            var binding = Binding;
            if (binding == null)
                return false;
            if (message == null)
                return true;
            if (MemberNameEqual(message.PropertyName, binding.SourceAccessor))
                UpdateErrors();
            return true;
        }

        private bool MemberNameEqual(string memberName, IBindingSourceAccessor accessor)
        {
            if (string.IsNullOrEmpty(memberName))
                return true;
            var paths = ErrorPaths;
            bool hasPaths = paths != null && paths.Length != 0;
            if (hasPaths)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (ToolkitExtensions.PropertyNameEqual(memberName, paths[i], true))
                        return true;
                }
            }
            var singleAccessor = accessor as ISingleBindingSourceAccessor;
            string path;
            if (singleAccessor != null)
            {
                path = singleAccessor.Source.Path.Parts.LastOrDefault();
                if (hasPaths && string.IsNullOrEmpty(path))
                    return false;
                return ToolkitExtensions.PropertyNameEqual(memberName, path, true);
            }
            for (int i = 0; i < accessor.Sources.Count; i++)
            {
                path = accessor.Sources[i].Path.Parts.LastOrDefault();
                if (hasPaths && string.IsNullOrEmpty(path))
                    continue;
                if (ToolkitExtensions.PropertyNameEqual(memberName, path, true))
                    return true;
            }
            return false;
        }

        #endregion

        #region Implementation of interfaces

        bool IEventListener.IsAlive
        {
            get { return Binding != null; }
        }

        bool IEventListener.IsWeak
        {
            get { return false; }
        }

        bool IEventListener.TryHandle(object sender, object message)
        {
            return Handle(message as DataErrorsChangedEventArgs);
        }

        WeakReference IHasWeakReference.WeakReference
        {
            get { return _selfReference; }
        }

        #endregion
    }
}