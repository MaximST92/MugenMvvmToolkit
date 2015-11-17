﻿#region Copyright

// ****************************************************************************
// <copyright file="JavaObjectWeakReference.cs">
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
using Android.Runtime;

namespace MugenMvvmToolkit.Android.Models
{
    //see https://bugzilla.xamarin.com/show_bug.cgi?id=16343
    internal sealed class JavaObjectWeakReference : WeakReference
    {
        #region Constructors

        public JavaObjectWeakReference(IJavaObject item)
            : base(item, true)
        {
        }

        #endregion

        #region Overrides of WeakReference

        public override bool IsAlive
        {
            get { return Target != null; }
        }

        public override object Target
        {
            get
            {
                var target = (IJavaObject)base.Target;
                if (target == null)
                    return null;
                if (target.Handle == IntPtr.Zero)
                {
                    base.Target = null;
                    return null;
                }
                return target;
            }
            set
            {
                if (value != null)
                    throw new NotSupportedException();
                base.Target = null;
            }
        }

        #endregion
    }
}