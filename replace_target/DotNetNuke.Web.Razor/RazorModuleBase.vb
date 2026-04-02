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

Imports System.Web.Compilation
Imports DotNetNuke.UI.Modules
Imports System.Web.UI
Imports System.Globalization
Imports System.IO

Imports DotNetNuke.Services.Exceptions
Imports DotNetNuke.Web.Razor.Helpers
Imports System.Web.WebPages
Imports System.Web

Namespace DotNetNuke.Web.Razor
    Public Class RazorModuleBase
        Inherits ModuleUserControlBase

        Private m_RazorScriptFile

        Protected ReadOnly Property HttpContext As HttpContextBase
            Get
                Return New HttpContextWrapper(System.Web.HttpContext.Current)
            End Get
        End Property

        Private Function CreateWebPageInstance() As Object
            Dim type As Type = BuildManager.GetCompiledType(RazorScriptFile)
            Dim instance As Object = Nothing

            If type IsNot Nothing Then
                instance = Activator.CreateInstance(type)
            End If

            Return instance
        End Function

        Private Sub InitHelpers(ByVal webPage As DotNetNukeWebPage)
            webPage.Dnn = New DnnHelper(ModuleContext)
            webPage.Html = New HtmlHelper(ModuleContext, LocalResourceFile)
            webPage.Url = New UrlHelper(ModuleContext)
        End Sub

        Protected Overridable ReadOnly Property RazorScriptFile As String
            Get
                Dim scriptFolder As String = Me.AppRelativeTemplateSourceDirectory
                Dim fileRoot As String = Path.GetFileNameWithoutExtension(Me.AppRelativeVirtualPath)
                Dim scriptFile As String = scriptFolder + "_" + fileRoot + ".cshtml"

                If Not File.Exists(Server.MapPath(scriptFile)) Then
                    'Try VB (vbhtml)
                    scriptFile = scriptFolder + "_" + fileRoot + ".vbhtml"
                End If

                If Not File.Exists(Server.MapPath(scriptFile)) Then
                    'Return ""
                    scriptFile = ""
                End If
                Return scriptFile
            End Get
        End Property

        Protected Overrides Sub OnPreRender(ByVal e As System.EventArgs)
            MyBase.OnPreRender(e)

            Try
                If Not String.IsNullOrEmpty(RazorScriptFile) Then

                    Dim instance As Object = CreateWebPageInstance()
                    If instance Is Nothing Then
                        Throw New InvalidOperationException( _
                            String.Format( _
                                CultureInfo.CurrentCulture, _
                                "The webpage found at '{0}' was not created.", _
                                RazorScriptFile))
                    End If

                    Dim webPage As DotNetNukeWebPage = TryCast(instance, DotNetNukeWebPage)

                    If webPage Is Nothing Then
                        Throw New InvalidOperationException( _
                            String.Format( _
                                CultureInfo.CurrentCulture,
                                "The webpage at '{0}' must derive from DotNetNukeWebPage.",
                                RazorScriptFile))
                    End If

                    webPage.Context = HttpContext
                    webPage.VirtualPath = VirtualPathUtility.GetDirectory(RazorScriptFile)
                    InitHelpers(webPage)

                    Dim writer As New StringWriter
                    webPage.ExecutePageHierarchy(New WebPageContext(HttpContext, webPage, Nothing), writer, webPage)

                    Controls.Add(New LiteralControl(Server.HtmlDecode(writer.ToString())))
                End If
            Catch ex As Exception
                ProcessModuleLoadException(Me, ex)
            End Try


        End Sub

    End Class
End Namespace
