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

Imports DotNetNuke.Web.Razor.Helpers
Imports DotNetNuke.Entities.Portals
Imports DotNetNuke.Entities.Tabs
Imports DotNetNuke.UI.Modules
Imports DotNetNuke.Entities.Modules
Imports DotNetNuke.Entities.Users
Imports System.Web.WebPages

Namespace DotNetNuke.Web.Razor
    Public MustInherit Class DotNetNukeWebPage
        Inherits WebPageBase

        Private m_Dnn As DnnHelper
        Private m_Html As HtmlHelper
        Private m_Url As UrlHelper


#Region "Helpers"

        Protected Friend Property Dnn As DnnHelper
            Get
                Return m_Dnn
            End Get
            Friend Set(ByVal value As DnnHelper)
                m_Dnn = value
            End Set
        End Property

        Protected Friend Property Html As HtmlHelper
            Get
                Return m_Html
            End Get
            Friend Set(ByVal value As HtmlHelper)
                m_Html = value
            End Set
        End Property

        Protected Friend Property Url As UrlHelper
            Get
                Return m_Url
            End Get
            Friend Set(ByVal value As UrlHelper)
                m_Url = value
            End Set
        End Property

#End Region

#Region "BaseClass Overrides"

        Protected Overrides Sub ConfigurePage(ByVal parentPage As System.Web.WebPages.WebPageBase)
            MyBase.ConfigurePage(parentPage)

            'Child pages need to get their context from the Parent
            Me.Context = parentPage.Context
        End Sub

#End Region

    End Class
End Namespace

