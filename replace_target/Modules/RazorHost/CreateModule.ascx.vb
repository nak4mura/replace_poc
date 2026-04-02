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
Imports DotNetNuke.Services.Installer
Imports DotNetNuke.Entities.Tabs
Imports Telerik.Web.UI
Imports DotNetNuke.Web.UI.WebControls

Imports DotNetNuke.UI.Skins
Imports DotNetNuke.Entities.Modules.Definitions

Namespace DotNetNuke.Modules.RazorHost

    Public Class CreateModule
        Inherits ModuleUserControlBase

        Private razorScriptFileFormatString As String = "~/DesktopModules/RazorModules/RazorHost/Scripts/{0}"
        Private razorScriptFolder As String = "~/DesktopModules/RazorModules/RazorHost/Scripts/"

        Protected ReadOnly Property ModuleControl As String
            Get
                Return Path.GetFileNameWithoutExtension(scriptList.SelectedValue).TrimStart("_"c) + ".ascx"
            End Get
        End Property

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

        Private Sub CreateModule()
            'Create new Folder
            Dim folderMapPath As String = Server.MapPath(String.Format("~/DesktopModules/RazorModules/{0}", txtFolder.Text))
            If Directory.Exists(folderMapPath) Then
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, Localization.GetString("FolderExists", Me.LocalResourceFile), ModuleMessage.ModuleMessageType.RedError)
                Exit Sub
            Else
                'Create folder
                Directory.CreateDirectory(folderMapPath)
            End If

            'Create new Module Control
            Dim moduleControlMapPath As String = folderMapPath + "/" + ModuleControl
            Try
                Using moduleControlWriter As New StreamWriter(moduleControlMapPath)
                    moduleControlWriter.Write(Localization.GetString("ModuleControlText.Text", Me.LocalResourceFile))
                    moduleControlWriter.Flush()
                End Using
            Catch ex As Exception
                LogException(ex)
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, Localization.GetString("ModuleControlCreationError", Me.LocalResourceFile), ModuleMessage.ModuleMessageType.RedError)
                Exit Sub
            End Try

            'Copy Script to new Folder
            Dim scriptSourceFile As String = Server.MapPath(String.Format(razorScriptFileFormatString, scriptList.SelectedValue))
            Dim scriptTargetFile As String = folderMapPath + "/" + scriptList.SelectedValue
            Try
                File.Copy(scriptSourceFile, scriptTargetFile)
            Catch ex As Exception
                LogException(ex)
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, Localization.GetString("ScriptCopyError", Me.LocalResourceFile), ModuleMessage.ModuleMessageType.RedError)
                Exit Sub
            End Try

            'Create new Manifest in target folder
            Dim manifestMapPath As String = folderMapPath + "/" + ModuleControl.Replace(".ascx", ".dnn")
            Try
                Using manifestWriter As New StreamWriter(manifestMapPath)
                    Dim manifestTemplate As String = Localization.GetString("ManifestText.Text", Me.LocalResourceFile)
                    Dim manifest As String = String.Format(manifestTemplate, txtName.Text, txtDescription.Text, _
                                                            txtFolder.Text, ModuleControl, scriptList.SelectedValue)
                    manifestWriter.Write(manifest)
                    manifestWriter.Flush()
                End Using
            Catch ex As Exception
                LogException(ex)
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, Localization.GetString("ManifestCreationError", Me.LocalResourceFile), ModuleMessage.ModuleMessageType.RedError)
                Exit Sub
            End Try

            'Register Module
            Dim moduleDefinition As ModuleDefinitionInfo = ImportManifest(manifestMapPath)

            'Optionally goto new Page
            If chkAddPage.Checked Then
                Dim tabName As String = "Test " + txtName.Text + " Page"
                Dim tabPath As String = GenerateTabPath(Null.NullInteger, tabName)
                Dim tabID As Integer = TabController.GetTabByTabPath(Me.ModuleContext.PortalId, tabPath, Me.ModuleContext.PortalSettings.CultureCode)

                If tabID = Null.NullInteger Then
                    'Create a new page
                    Dim newTab As New DotNetNuke.Entities.Tabs.TabInfo
                    newTab.TabName = "Test " + txtName.Text + " Page"
                    newTab.ParentId = Null.NullInteger
                    newTab.PortalID = Me.ModuleContext.PortalId
                    newTab.IsVisible = True
                    newTab.TabID = New TabController().AddTabBefore(newTab, Me.ModuleContext.PortalSettings.AdminTabId)

                    Dim objModule As New ModuleInfo
                    objModule.Initialize(Me.ModuleContext.PortalId)

                    objModule.PortalID = Me.ModuleContext.PortalId
                    objModule.TabID = newTab.TabID
                    objModule.ModuleOrder = Null.NullInteger
                    objModule.ModuleTitle = moduleDefinition.FriendlyName
                    objModule.PaneName = glbDefaultPane
                    objModule.ModuleDefID = moduleDefinition.ModuleDefID
                    objModule.InheritViewPermissions = True
                    objModule.AllTabs = False
                    Dim moduleCtl As New ModuleController
                    moduleCtl.AddModule(objModule)

                    Response.Redirect(NavigateURL(newTab.TabID), True)
                Else
                    DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, Localization.GetString("TabExists", Me.LocalResourceFile), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.RedError)
                End If

            Else
                'Redirect to main extensions page
                Response.Redirect(NavigateURL(), True)
            End If
        End Sub

        Private Function ImportManifest(ByVal manifest As String) As ModuleDefinitionInfo
            Dim moduleDefinition As ModuleDefinitionInfo = Nothing
            Try
                Dim _Installer As New Installer(manifest, Request.MapPath("."), True)

                If _Installer.IsValid Then
                    'Reset Log
                    _Installer.InstallerInfo.Log.Logs.Clear()

                    'Install
                    _Installer.Install()

                    If _Installer.IsValid Then
                        Dim desktopModule As DesktopModuleInfo = DesktopModuleController.GetDesktopModuleByPackageID(_Installer.InstallerInfo.PackageID)
                        If desktopModule IsNot Nothing AndAlso desktopModule.ModuleDefinitions.Count > 0 Then
                            For Each kvp As KeyValuePair(Of String, ModuleDefinitionInfo) In desktopModule.ModuleDefinitions
                                moduleDefinition = kvp.Value
                                Exit For
                            Next
                        End If
                    Else
                        DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, Services.Localization.Localization.GetString("InstallError.Text", Me.LocalResourceFile), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.RedError)
                        phInstallLogs.Controls.Add(_Installer.InstallerInfo.Log.GetLogsTable)
                    End If
                Else
                    DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, Services.Localization.Localization.GetString("InstallError.Text", Me.LocalResourceFile), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.RedError)
                    phInstallLogs.Controls.Add(_Installer.InstallerInfo.Log.GetLogsTable)
                End If
            Catch exc As Exception
                LogException(exc)
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, Services.Localization.Localization.GetString("ImportControl.ErrorMessage", Me.LocalResourceFile), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.RedError)
            End Try

            Return moduleDefinition
        End Function

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
            Dim scriptFile As String = String.Format(razorScriptFileFormatString, scriptList.SelectedValue)
            Dim srcFile As String = Server.MapPath(scriptFile)

            lblSourceFile.Text = String.Format(Localization.GetString("SourceFile", Me.LocalResourceFile), scriptFile)
            lblModuleControl.Text = String.Format(Localization.GetString("SourceControl", Me.LocalResourceFile), ModuleControl)
        End Sub

        Private Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not Me.ModuleContext.PortalSettings.UserInfo.IsSuperUser Then
                Response.Redirect(NavigateURL("Access Denied"), True)
            End If

            If Not Page.IsPostBack Then
                LoadScripts()
                DisplayFile()
            End If
        End Sub

        Private Sub cmdCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancel.Click
            Try
                Response.Redirect(NavigateURL(), True)
            Catch exc As Exception    'Module failed to load
                ProcessModuleLoadException(Me, exc)
            End Try
        End Sub

        Private Sub cmdCreate_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdCreate.Click
            Try
                If Not Me.ModuleContext.PortalSettings.UserInfo.IsSuperUser Then
                    Response.Redirect(NavigateURL("Access Denied"), True)
                End If

                If Page.IsValid Then
                    CreateModule()
                End If
            Catch exc As Exception    'Module failed to load
                ProcessModuleLoadException(Me, exc)
            End Try
        End Sub

        Private Sub scriptList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles scriptList.SelectedIndexChanged
            DisplayFile()
        End Sub

    End Class

End Namespace
