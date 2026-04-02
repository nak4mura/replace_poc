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

Imports DotNetNuke.Entities.Modules.Actions
Imports DotNetNuke.Web.Razor
Imports DotNetNuke.Entities.Modules
Imports DotNetNuke.Services.Localization
Imports DotNetNuke.Security

Namespace DotNetNuke.Modules.RazorHost

    Public Class RazorHost
        Inherits RazorModuleBase
        Implements IActionable

        Private razorScriptFileFormatString As String = "~/DesktopModules/RazorModules/RazorHost/Scripts/{0}"

        Protected Overrides ReadOnly Property RazorScriptFile As String
            Get
                Dim m_RazorScriptFile As String = MyBase.RazorScriptFile
                Dim scriptFileSetting As String = TryCast(ModuleContext.Settings("ScriptFile"), String)
                If Not String.IsNullOrEmpty(scriptFileSetting) Then
                    m_RazorScriptFile = String.Format(razorScriptFileFormatString, scriptFileSetting)
                End If
                Return m_RazorScriptFile
            End Get
        End Property

        Public ReadOnly Property ModuleActions() As ModuleActionCollection Implements IActionable.ModuleActions
            Get
                Dim Actions As New ModuleActionCollection
                Actions.Add(ModuleContext.GetNextActionID, Localization.GetString(ModuleActionType.EditContent, LocalResourceFile), ModuleActionType.AddContent, "", "edit.gif", ModuleContext.EditUrl(), False, SecurityAccessLevel.Host, True, False)
                Actions.Add(ModuleContext.GetNextActionID, Localization.GetString("CreateModule.Action", LocalResourceFile), ModuleActionType.AddContent, "", "edit.gif", ModuleContext.EditUrl("CreateModule"), False, SecurityAccessLevel.Host, True, False)
                Return Actions
            End Get
        End Property

    End Class

End Namespace
