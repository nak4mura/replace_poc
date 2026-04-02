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
Imports DotNetNuke.Entities.Modules
Imports Telerik.Web.UI
Imports DotNetNuke.Web.UI.WebControls

Namespace DotNetNuke.Modules.RazorHost

    Public Class EditScript
        Inherits ModuleUserControlBase

        Private razorScriptFileFormatString As String = "~/DesktopModules/RazorModules/RazorHost/Scripts/{0}"
        Private razorScriptFolder As String = "~/DesktopModules/RazorModules/RazorHost/Scripts/"

        Protected ReadOnly Property RazorScriptFile As String
            Get
                Dim m_RazorScriptFile As String = Null.NullString
                Dim scriptFileSetting As String = TryCast(ModuleContext.Settings("ScriptFile"), String)
                If Not String.IsNullOrEmpty(scriptFileSetting) Then
                    m_RazorScriptFile = String.Format(razorScriptFileFormatString, scriptFileSetting)
                End If
                Return m_RazorScriptFile
            End Get
        End Property

        Private Sub LoadScripts()
            Dim basePath As String = Server.MapPath(razorScriptFolder)
            Dim scriptFileSetting As String = TryCast(ModuleContext.Settings("ScriptFile"), String)

            For Each script As String In Directory.GetFiles(Server.MapPath(razorScriptFolder), "*.??html")
                Dim scriptPath As String = script.Replace(basePath, "")
                Dim item As New ListItem(scriptPath, scriptPath)
                If Not (String.IsNullOrEmpty(scriptFileSetting)) AndAlso scriptPath.ToLowerInvariant = scriptFileSetting.ToLowerInvariant Then
                    item.Selected = True
                End If
                scriptList.Items.Add(item)
            Next

        End Sub

        Private Sub DisplayFile()
            Dim scriptFileSetting As String = TryCast(ModuleContext.Settings("ScriptFile"), String)
            Dim scriptFile As String = String.Format(razorScriptFileFormatString, scriptList.SelectedValue)
            Dim srcFile As String = Server.MapPath(scriptFile)

            lblSourceFile.Text = String.Format(Localization.GetString("SourceFile", Me.LocalResourceFile), scriptFile)

            Dim objStreamReader As StreamReader
            objStreamReader = File.OpenText(srcFile)
            txtSource.Text = objStreamReader.ReadToEnd
            objStreamReader.Close()

            If Not String.IsNullOrEmpty(scriptFileSetting) Then
                isCurrentScript.Checked = (scriptList.SelectedValue.ToLowerInvariant = scriptFileSetting.ToLowerInvariant)
            End If
        End Sub

        Private Sub SaveScript()
            Dim srcFile As String = Server.MapPath(String.Format(razorScriptFileFormatString, scriptList.SelectedValue))

            ' write file
            Dim objStream As StreamWriter
            objStream = File.CreateText(srcFile)
            objStream.WriteLine(txtSource.Text)
            objStream.Close()

            If isCurrentScript.Checked Then
                'Update setting
                Dim controller As New ModuleController
                controller.UpdateModuleSetting(ModuleContext.ModuleId, "ScriptFile", scriptList.SelectedValue)
            End If
        End Sub

        Private Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not Page.IsPostBack Then
                LoadScripts()
                DisplayFile()
            End If

            'Add the enable content Localization Button to the ToolTip Manager
            toolTipManager.TargetControls.Add(addNewButton.ID)
        End Sub

        Private Sub cmdCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancel.Click
            Try
                Response.Redirect(NavigateURL(), True)
            Catch exc As Exception    'Module failed to load
                ProcessModuleLoadException(Me, exc)
            End Try
        End Sub

        Private Sub cmdSave_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdSave.Click
            Try
                SaveScript()
            Catch exc As Exception    'Module failed to load
                ProcessModuleLoadException(Me, exc)
            End Try
        End Sub

        Private Sub cmdSaveClose_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdSaveClose.Click
            Try
                SaveScript()
                Response.Redirect(NavigateURL(), True)
            Catch exc As Exception    'Module failed to load
                ProcessModuleLoadException(Me, exc)
            End Try
        End Sub

        Private Sub scriptList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles scriptList.SelectedIndexChanged
            DisplayFile()
        End Sub

        Protected Sub toolTipManager_AjaxUpdate(ByVal sender As Object, ByVal e As ToolTipUpdateEventArgs) Handles toolTipManager.AjaxUpdate
            Dim ctrl As Control = Page.LoadControl("~/DesktopModules/RazorModules/RazorHost/AddScript.ascx")
            e.UpdatePanel.ContentTemplateContainer.Controls.Add(ctrl)
        End Sub

    End Class

End Namespace
