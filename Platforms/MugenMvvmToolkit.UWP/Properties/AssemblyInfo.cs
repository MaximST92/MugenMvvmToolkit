﻿#region Copyright

// ****************************************************************************
// <copyright file="AssemblyInfo.cs">
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

//#define SIGNASSEMBLY

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("MugenMvvmToolkit.UAP")]
[assembly: AssemblyProduct("MugenMvvmToolkit.UAP")]
#if SIGNASSEMBLY
[assembly: InternalsVisibleTo("System.Runtime.Serialization, PublicKey=00000000000000000400000000000000")]
[assembly: InternalsVisibleTo("MugenMvvmToolkit.Test, PublicKey=00240000048000009400000006020000002400005253413100040000010001005515f32c95d7ac72020d117fa0a25b0dc2be6483827ffe624875b838c9e02c43345e566d5159437f1eddf9ae9f16075f1a971394c1d0975b79a553bad5c28af1dbf1e36f5b5b0142918b33a90884e1621ef8bfc2a2433ab406e159cc3ad2ecb792686efaae6a60f7cf69016ca98ee2b0ebf8dcc85f9f0c0e21d22a1827b830aa")]
#else
[assembly: InternalsVisibleTo("System.Runtime.Serialization")]
[assembly: InternalsVisibleTo("MugenMvvmToolkit.Test")]
#endif