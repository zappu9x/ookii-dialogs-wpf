﻿#region Copyright 2009-2021 Ookii Dialogs Contributors
//
// Licensed under the BSD 3-Clause License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://opensource.org/licenses/BSD-3-Clause
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.IO;
using Ookii.Dialogs.Wpf.Interop;
using System.Windows.Interop;
using System.Windows;
using System.Runtime.InteropServices;
using System.Linq;

namespace Ookii.Dialogs.Wpf
{
    /// <summary>
    /// Prompts the user to select a folder.
    /// </summary>
    /// <remarks>
    /// This class will use the Vista style Select Folder dialog if possible, or the regular FolderBrowserDialog
    /// if it is not. Note that the Vista style dialog is very different, so using this class without testing
    /// in both Vista and older Windows versions is not recommended.
    /// </remarks>
    /// <threadsafety instance="false" static="true" />
    [DefaultEvent("HelpRequest"), Designer("System.Windows.Forms.Design.FolderBrowserDialogDesigner, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), DefaultProperty("SelectedPath"), Description("Prompts the user to select a folder.")]
    public sealed class VistaFolderBrowserDialog
    {
        private string _description;
        private string _selectedPath;
        private NativeMethods.FOS _options;
        private string[] _selectedPaths;

        /// <summary>
        /// Creates a new instance of the <see cref="VistaFolderBrowserDialog" /> class.
        /// </summary>
        public VistaFolderBrowserDialog()
        {
            Reset();
        }

        #region Public Properties

        /// <summary>
        /// Gets a value that indicates whether the current OS supports Vista-style common file dialogs.
        /// </summary>
        /// <value>
        /// <see langword="true" /> on Windows Vista or newer operating systems; otherwise, <see langword="false" />.
        /// </value>
        [Browsable(false)]
        public static bool IsVistaFolderDialogSupported
        {
            get
            {
                return NativeMethods.IsWindowsVistaOrLater;
            }
        }

        /// <summary>
        /// Gets or sets the descriptive text displayed above the tree view control in the dialog box, or below the list view control
        /// in the Vista style dialog.
        /// </summary>
        /// <value>
        /// The description to display. The default is an empty string ("").
        /// </value>
        [Category("Folder Browsing"), DefaultValue(""), Localizable(true), Browsable(true), Description("The descriptive text displayed above the tree view control in the dialog box, or below the list view control in the Vista style dialog.")]
        public string Description
        {
            get
            {
                return _description ?? string.Empty;
            }
            set
            {
                _description = value;
            }
        }

        /// <summary>
        /// Gets or sets the root folder where the browsing starts from. This property has no effect if the Vista style
        /// dialog is used.
        /// </summary>
        /// <value>
        /// One of the <see cref="System.Environment.SpecialFolder" /> values. The default is Desktop.
        /// </value>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">The value assigned is not one of the <see cref="System.Environment.SpecialFolder" /> values.</exception>
        [Localizable(false), Description("The root folder where the browsing starts from. This property has no effect if the Vista style dialog is used."), Category("Folder Browsing"), Browsable(true), DefaultValue(typeof(System.Environment.SpecialFolder), "Desktop")]
        public System.Environment.SpecialFolder RootFolder { get; set; }
	
        /// <summary>
        /// Gets or sets the path selected by the user.
        /// </summary>
        /// <value>
        /// The path of the folder first selected in the dialog box or the last folder selected by the user. The default is an empty string ("").
        /// </value>
        [Browsable(true), Editor("System.Windows.Forms.Design.SelectedPathEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(System.Drawing.Design.UITypeEditor)), Description("The path selected by the user."), DefaultValue(""), Localizable(true), Category("Folder Browsing")]
        public string SelectedPath
        {
            get
            {
                var selectedPath =
                    _selectedPath ??
                    _selectedPaths?.FirstOrDefault() ??
                    string.Empty;

                return selectedPath;
            }

            set
            {
                _selectedPath = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the New Folder button appears in the folder browser dialog box. This
        /// property has no effect if the Vista style dialog is used; in that case, the New Folder button is always shown.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the New Folder button is shown in the dialog box; otherwise, <see langword="false" />. The default is <see langword="true" />.
        /// </value>
        [Browsable(true), Localizable(false), Description("A value indicating whether the New Folder button appears in the folder browser dialog box. This property has no effect if the Vista style dialog is used; in that case, the New Folder button is always shown."), DefaultValue(true), Category("Folder Browsing")]
        public bool ShowNewFolderButton { get; set; }	

        /// <summary>
        /// Gets or sets a value that indicates whether to use the value of the <see cref="Description" /> property
        /// as the dialog title for Vista style dialogs. This property has no effect on old style dialogs.
        /// </summary>
        /// <value><see langword="true" /> to indicate that the value of the <see cref="Description" /> property is used as dialog title; <see langword="false" />
        /// to indicate the value is added as additional text to the dialog. The default is <see langword="false" />.</value>
        [Category("Folder Browsing"), DefaultValue(false), Description("A value that indicates whether to use the value of the Description property as the dialog title for Vista style dialogs. This property has no effect on old style dialogs.")]
        public bool UseDescriptionForTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog box allows multiple folder to be selected.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the dialog box allows multiple folder to be selected together or concurrently; otherwise, <see langword="false" />. 
        /// The default value is <see langword="false" />.
        /// </value>
        [Description("A value indicating whether the dialog box allows multiple folders to be selected."), DefaultValue(false), Category("Behavior")]
        public bool Multiselect
        {
            get
            {
                return HasOption(NativeMethods.FOS.FOS_ALLOWMULTISELECT);
            }

            set
            {
                SetOption(NativeMethods.FOS.FOS_ALLOWMULTISELECT, value);
            }
        }

        /// <summary>
        /// Gets the folder paths of all selected folder in the dialog box.
        /// </summary>
        /// <value>
        /// An array of type <see cref="string"/>, containing the folder paths of all selected folder in the dialog box.
        /// </value>
        [Description("The folder path of all selected folder in the dialog box."), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string[] SelectedPaths
        {
            get
            {
                var selectedPaths = _selectedPaths;

                if (selectedPaths is null)
                {
                    var selectedPath = _selectedPath;
                    if (string.IsNullOrWhiteSpace(selectedPath))
                    {
                        return new string[0];
                    }
                    else
                    {
                        return new[] { selectedPath };
                    }
                }

                return (string[])selectedPaths.Clone();
            }

            set
            {
                _selectedPaths = value;
            }
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// Resets all properties to their default values.
        /// </summary>
        public void Reset()
        {
            _description = string.Empty;
            UseDescriptionForTitle = false;
            _selectedPath = string.Empty;
            RootFolder = Environment.SpecialFolder.Desktop;
            ShowNewFolderButton = true;
            _selectedPaths = null;
            _options = 0;
        }

        /// <summary>
        /// Displays the folder browser dialog.
        /// </summary>
        /// <returns>If the user clicks the OK button, <see langword="true" /> is returned; otherwise, <see langword="false" />.</returns>
        public bool? ShowDialog()
        {
            return ShowDialog(null);
        }

        /// <summary>
        /// Displays the folder browser dialog.
        /// </summary>
        /// <param name="owner">Handle to the window that owns the dialog.</param>
        /// <returns>If the user clicks the OK button, <see langword="true" /> is returned; otherwise, <see langword="false" />.</returns>
        public bool? ShowDialog(Window owner)
        {
            IntPtr ownerHandle = owner == null ? NativeMethods.GetActiveWindow() : new WindowInteropHelper(owner).Handle;
            return ShowDialog(ownerHandle);
        }

        /// <summary>
        /// Displays the folder browser dialog.
        /// </summary>
        /// <param name="owner">The <see cref="IntPtr"/> Win32 handle that is the owner of this dialog.</param>
        /// <returns>If the user clicks the OK button, <see langword="true" /> is returned; otherwise, <see langword="false" />.</returns>
        public bool? ShowDialog(IntPtr owner)
        {
            IntPtr ownerHandle = owner == default(IntPtr) ? NativeMethods.GetActiveWindow() : owner;
            return new bool?(IsVistaFolderDialogSupported ? RunDialog(ownerHandle) : RunDialogDownlevel(ownerHandle));
        }

        #endregion

        #region Internal Methods

        internal void SetOption(NativeMethods.FOS option, bool value)
        {
            if (value)
            {
                _options |= option;
            }
            else
            {
                _options &= ~option;
            }
        }

        internal bool HasOption(NativeMethods.FOS option)
        {
            return (_options & option) != 0;
        }

        #endregion

        #region Private Methods

        private bool RunDialog(IntPtr owner)
        {
            Ookii.Dialogs.Wpf.Interop.IFileDialog dialog = null;
            try
            {
                dialog = new Ookii.Dialogs.Wpf.Interop.NativeFileOpenDialog();
                SetDialogProperties(dialog);
                int result = dialog.Show(owner);
                if( result < 0 )
                {
                    if( (uint)result == (uint)HRESULT.ERROR_CANCELLED )
                        return false;
                    else
                        throw System.Runtime.InteropServices.Marshal.GetExceptionForHR(result);
                } 
                GetResult(dialog);
                return true;
            }
            finally
            {
                if( dialog != null )
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dialog);
            }
        }

        private bool RunDialogDownlevel(IntPtr owner)
        {
            IntPtr rootItemIdList = IntPtr.Zero;
            IntPtr resultItemIdList = IntPtr.Zero;
            if( NativeMethods.SHGetSpecialFolderLocation(owner, RootFolder, ref rootItemIdList) != 0 )
            {
                if( NativeMethods.SHGetSpecialFolderLocation(owner, 0, ref rootItemIdList) != 0 )
                {
                    throw new InvalidOperationException(Properties.Resources.FolderBrowserDialogNoRootFolder);
                }
            }
            try
            {
                NativeMethods.BROWSEINFO info = new NativeMethods.BROWSEINFO();
                info.hwndOwner = owner;
                info.lpfn = new NativeMethods.BrowseCallbackProc(BrowseCallbackProc);
                info.lpszTitle = Description;
                info.pidlRoot = rootItemIdList;
                info.pszDisplayName = new string('\0', 260);
                info.ulFlags = NativeMethods.BrowseInfoFlags.NewDialogStyle | NativeMethods.BrowseInfoFlags.ReturnOnlyFsDirs;
                if( !ShowNewFolderButton )
                    info.ulFlags |= NativeMethods.BrowseInfoFlags.NoNewFolderButton;
                resultItemIdList = NativeMethods.SHBrowseForFolder(ref info);
                if( resultItemIdList != IntPtr.Zero )
                {
                    StringBuilder path = new StringBuilder(260);
                    NativeMethods.SHGetPathFromIDList(resultItemIdList, path);
                    SelectedPath = path.ToString();
                    return true;
                }
                else
                    return false;
            }
            finally
            {
                if( rootItemIdList != IntPtr.Zero )
                {
                    IMalloc malloc = NativeMethods.SHGetMalloc();
                    malloc.Free(rootItemIdList);
                    Marshal.ReleaseComObject(malloc);
                }
                if( resultItemIdList != IntPtr.Zero )
                {
                    Marshal.FreeCoTaskMem(resultItemIdList);
                }
            }
        }

        private void SetDialogProperties(Ookii.Dialogs.Wpf.Interop.IFileDialog dialog)
        {
            // Description
            if( !string.IsNullOrEmpty(_description) )
            {
                if( UseDescriptionForTitle )
                {
                    dialog.SetTitle(_description);
                }
                else
                {
                    Ookii.Dialogs.Wpf.Interop.IFileDialogCustomize customize = (Ookii.Dialogs.Wpf.Interop.IFileDialogCustomize)dialog;
                    customize.AddText(0, _description);
                }
            }

            // Set the default directory
            if (Directory.Exists(_selectedPath))
            {
                dialog.SetFolder(NativeMethods.CreateItemFromParsingName(_selectedPath));
            }

            dialog.SetOptions(NativeMethods.FOS.FOS_PICKFOLDERS | NativeMethods.FOS.FOS_FORCEFILESYSTEM | NativeMethods.FOS.FOS_FILEMUSTEXIST);
        }

        private void GetResult(IFileDialog dialog)
        {
            if (Multiselect)
            {
                ((IFileOpenDialog)dialog).GetResults(out IShellItemArray results);

                results.GetCount(out uint count);
                string[] folderPaths = new string[count];

                for (uint x = 0; x < count; ++x)
                {
                    results.GetItemAt(x, out IShellItem item);
                    item.GetDisplayName(NativeMethods.SIGDN.SIGDN_FILESYSPATH, out string name);

                    folderPaths[x] = name;
                }

                SelectedPaths = folderPaths;
            }
            else
            {
                dialog.GetResult(out IShellItem item);
                item.GetDisplayName(NativeMethods.SIGDN.SIGDN_FILESYSPATH, out _selectedPath);
            }
        }

        private int BrowseCallbackProc(IntPtr hwnd, NativeMethods.FolderBrowserDialogMessage msg, IntPtr lParam, IntPtr wParam)
        {
            switch( msg )
            {
            case NativeMethods.FolderBrowserDialogMessage.Initialized:
                if( SelectedPath.Length != 0 )
                    NativeMethods.SendMessage(hwnd, NativeMethods.FolderBrowserDialogMessage.SetSelection, new IntPtr(1), SelectedPath);
                break;
            case NativeMethods.FolderBrowserDialogMessage.SelChanged:
                if( lParam != IntPtr.Zero )
                {
                    StringBuilder path = new StringBuilder(260);
                    bool validPath = NativeMethods.SHGetPathFromIDList(lParam, path);
                    NativeMethods.SendMessage(hwnd, NativeMethods.FolderBrowserDialogMessage.EnableOk, IntPtr.Zero, validPath ? new IntPtr(1) : IntPtr.Zero);
                }
                break;
            }
            return 0;
        }

        #endregion
    }
}
