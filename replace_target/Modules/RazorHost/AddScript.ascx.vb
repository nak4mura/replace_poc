'
' DotNetNuke - http://www.dotnetnuke.com
' Copyright (c) 2002-2010
' by DotNetNuke Corporation
'
' Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
' documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
' the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
' to permit persons to whom the Software is furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in all copies or substantial portions 
' of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
' DEALINGS IN THE SOFTWARE.
'

Imports DotNetNuke.UI.Skins.Controls
Imports DotNetNuke.Web.Razor
Imports DotNetNuke.UI.Modules
Imports DotNetNuke.Services.Localization
Imports System.IO
Imports DotNetNuke.Common.Utilities
Imports DotNetNuke.Services.Exceptions
Imports DotNetNuke.Common

Namespace DotNetNuke.Modules.RazorHost

    Public Class AddScript
        Inherits ModuleUserControlBase

        Private razorScriptFileFormatString As String = "~/DesktopModules/RazorModules/RazorHost/Scripts/{0}"

        Protected Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init

            Me.LocalResourceFile = Services.Localization.Localization.GetResourceFile(Me, "AddScript.ascx")

        End Sub

        Private Sub DisplayExtension()
            fileExtension.Text = "." + scriptFileType.SelectedValue.ToLowerInvariant
        End Sub

        Private Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            DisplayExtension()
        End Sub

        Protected Sub cmdCancel_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdCancel.Click
            'Redirect to refresh page (and skinobjects)
            Response.Redirect(Request.RawUrl, True)
        End Sub

        Protected Sub cmdUpdate_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdUpdate.Click
            Dim scriptFileName As String = "_" + Path.GetFileNameWithoutExtension(fileName.Text) + "." + scriptFileType.SelectedValue.ToLowerInvariant()

            Dim srcFile As String = Server.MapPath(String.Format(razorScriptFileFormatString, scriptFileName))

            ' write file
            Dim objStream As StreamWriter
            objStream = File.CreateText(srcFile)
            objStream.WriteLine(Localization.GetString("NewScript", Me.LocalResourceFile))
            objStream.Close()

            'Redirect to refresh page (and skinobjects)
            Response.Redirect(Request.RawUrl, True)
        End Sub

        Private Sub scriptFileType_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles scriptFileType.SelectedIndexChanged
            DisplayExtension()
        End Sub

    End Class

End Namespace
