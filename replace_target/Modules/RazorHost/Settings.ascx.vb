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
Imports DotNetNuke.Entities.Modules
Imports System.IO

Namespace DotNetNuke.Modules.RazorHost

    Public Class Settings
        Inherits ModuleSettingsBase

        Private razorScriptFolder As String = "~/DesktopModules/RazorModules/RazorHost/Scripts/"

        Public Overrides Sub LoadSettings()

            Dim basePath As String = Server.MapPath(razorScriptFolder)
            Dim scriptFileSetting As String = TryCast(Settings("ScriptFile"), String)

            For Each script As String In Directory.GetFiles(Server.MapPath(razorScriptFolder), "*.??html")
                Dim scriptPath As String = script.Replace(basePath, "")
                Dim item As New ListItem(scriptPath, scriptPath)
                If Not String.IsNullOrEmpty(scriptFileSetting) AndAlso scriptPath.ToLowerInvariant = scriptFileSetting.ToLowerInvariant Then
                    item.Selected = True
                End If
                scriptList.Items.Add(item)
            Next

            MyBase.LoadSettings()
        End Sub

        Public Overrides Sub UpdateSettings()

            Dim controller As New ModuleController
            controller.UpdateModuleSetting(ModuleId, "ScriptFile", scriptList.SelectedValue)
        End Sub

    End Class

End Namespace
