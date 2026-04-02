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
Imports DotNetNuke.Services.Localization
Imports DotNetNuke.UI.Modules
Imports DotNetNuke.Entities.Modules
Imports DotNetNuke.Entities.Portals
Imports DotNetNuke.Entities.Tabs
Imports DotNetNuke.Entities.Users

Namespace DotNetNuke.Web.Razor.Helpers
    Public Class DnnHelper

        Private _context As ModuleInstanceContext

        Public Sub New(ByVal context As ModuleInstanceContext)
            _context = context
        End Sub

        Public ReadOnly Property [Module] As ModuleInfo
            Get
                Return _context.Configuration
            End Get
        End Property

        Public ReadOnly Property Tab As TabInfo
            Get
                Return _context.PortalSettings.ActiveTab
            End Get
        End Property

        Public ReadOnly Property Portal As PortalSettings
            Get
                Return _context.PortalSettings
            End Get
        End Property

        Public ReadOnly Property User As UserInfo
            Get
                Return _context.PortalSettings.UserInfo
            End Get
        End Property

    End Class
End Namespace
