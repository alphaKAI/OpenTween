﻿// OpenTween - Client of Twitter
// Copyright (c) 2007-2012 kiri_feather (@kiri_feather) <kiri.feather@gmail.com>
//           (c) 2008-2012 Moz (@syo68k)
//           (c) 2008-2012 takeshik (@takeshik) <http://www.takeshik.org/>
//           (c) 2010-2012 anis774 (@anis774) <http://d.hatena.ne.jp/anis774/>
//           (c) 2010-2012 fantasticswallow (@f_swallow) <http://twitter.com/f_swallow>
//           (c) 2012      Egtra (@egtra) <http://dev.activebasic.com/egtra/>
//           (c) 2012      kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
// All rights reserved.
// 
// This file is part of OpenTween.
// 
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General public License as published by the Free
// Software Foundation; either version 3 of the License, or (at your option)
// any later version.
// 
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General public License
// for more details.
// 
// You should have received a copy of the GNU General public License along
// with this program. If not, see <http://www.gnu.org/licenses/>, or write to
// the Free Software Foundation, Inc., 51 Franklin Street - Fifth Floor,
// Boston, MA 02110-1301, USA.

using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Reflection;

namespace OpenTween
{
    internal class MyApplication
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static int Main()
        {
            CheckSettingFilePath();

            string pt = Application.ExecutablePath.Replace("\\", "/") + "/" + Application.ProductName;
            using (Mutex mt = new Mutex(false, pt))
            {
                if (!mt.WaitOne(0, false))
                {
                    ShowPreviousWindow();
                    return 1;
                }

                Application.ThreadException += MyApplication_UnhandledException;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TweenMain());

                mt.ReleaseMutex();

                return 0;
            }
        }

        private static void ShowPreviousWindow()
        {
            // 実行中の同じアプリケーションのウィンドウ・ハンドルの取得
            var prevProcess = Win32Api.GetPreviousProcess();
            if (prevProcess != null && prevProcess.MainWindowHandle == IntPtr.Zero)
            {
                // 起動中のアプリケーションを最前面に表示
                Win32Api.WakeupWindow(prevProcess.MainWindowHandle);
            }
            else
            {
                if (prevProcess != null)
                {
                    //プロセス特定は出来たが、ウィンドウハンドルが取得できなかった（アイコン化されている）
                    //タスクトレイアイコンのクリックをエミュレート
                    //注：アイコン特定はTooltipの文字列で行うため、多重起動時は先に見つけた物がアクティブになる
                    var rslt = Win32Api.ClickTasktrayIcon(Application.ProductName);
                    if (!rslt)
                    {
                        // 警告を表示（見つからない、またはその他の原因で失敗）
                        var text = string.Format(MyCommon.ReplaceAppName(Properties.Resources.StartupText1), MyCommon.GetAssemblyName());
                        MessageBox.Show(text, MyCommon.ReplaceAppName(Properties.Resources.StartupText2), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    // 警告を表示（プロセス見つからない場合）
                    var text = string.Format(MyCommon.ReplaceAppName(Properties.Resources.StartupText1), MyCommon.GetAssemblyName());
                    MessageBox.Show(text, MyCommon.ReplaceAppName(Properties.Resources.StartupText2), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private static void MyApplication_UnhandledException(object sender, ThreadExceptionEventArgs e)
        {
            //GDI+のエラー原因を特定したい
            if (e.Exception.Message != "A generic error occurred in GDI+." &&
               e.Exception.Message != "GDI+ で汎用エラーが発生しました。")
            {
                if (MyCommon.ExceptionOut(e.Exception))
                {
                    Application.Exit();
                }
            }
        }

        private static void CheckSettingFilePath()
        {
            if (File.Exists(Path.Combine(Application.StartupPath, "roaming")))
            {
                MyCommon.settingPath = MySpecialPath.UserAppDataPath();
            }
            else
            {
                MyCommon.settingPath = Application.StartupPath;
            }
        }
    }
}
