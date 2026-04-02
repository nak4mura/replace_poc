<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="CreateModule.ascx.vb" Inherits="DotNetNuke.Modules.RazorHost.CreateModule" %>
<%@ Register Assembly="DotnetNuke" Namespace="DotNetNuke.UI.WebControls" TagPrefix="dnn" %>
<%@ Register Assembly="DotnetNuke.Web" Namespace="DotNetNuke.Web.UI.WebControls" TagPrefix="dnn" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<dnn:DnnToolTipManager ID="toolTipManager" runat="server" Position="Center" RelativeTo="BrowserWindow"
    Width="500px" Height="200px" HideEvent="ManualClose" ShowEvent="OnClick" Modal="true"
    Skin="Default" RenderInPageRoot="true" AnimationDuration="200" ManualClose="true"
    ManualCloseButtonText="Close" />
<asp:RequiredFieldValidator ID="valFolder" runat="server" resourceKey="valFolder" ControlToValidate="txtFolder"
        CssClass="NormalRed" EnableClientScript="true" Display="Dynamic" />
<asp:RequiredFieldValidator ID="valName" runat="server" resourceKey="valName" ControlToValidate="txtName"
        CssClass="NormalRed" EnableClientScript="true" Display="Dynamic" />
<table>
    <tr>
        <td style="width:150px;" class="SubHead"><dnn:Label id="scriptsLabel" runat="Server" controlname="scriptList" /></td>
        <td style="width:600px;" class="NormalTextBox">
            <asp:DropDownList ID="scriptList" runat="server" AutoPostBack="true" />
        </td>
    </tr>
    <tr style="height:10px"><td>&nbsp;</td></tr>
    <tr>
        <td colspan="2"><asp:Label ID="lblSourceFile" runat="server" cssClass="NormalBold"/></td>
    </tr>
    <tr style="height:10px"><td>&nbsp;</td></tr>
    <tr>
        <td colspan="2"><asp:Label ID="lblModuleControl" runat="server" cssClass="NormalBold"/></td>
    </tr>
    <tr style="height:10px"><td>&nbsp;</td></tr>
    <tr>
        <td class="SubHead" style="width:175px" valign="top"><dnn:label id="plFolder" controlname="txtFolder" runat="server" /></td>
        <td valign="top" style="width:575px"><asp:TextBox ID="txtFolder" runat="server" width="300" CssClass="NormalTextBox" /></td>
    </tr>
    <tr>
        <td class="SubHead" style="width:175px" valign="top"><dnn:label id="plName" controlname="txtName" runat="server" /></td>
        <td valign="top" style="width:575px"><asp:TextBox ID="txtName" runat="server" width="300" CssClass="NormalTextBox" /></td>
    </tr>
    <tr>
        <td class="SubHead" style="width:175px" valign="top"><dnn:label id="plDescription" controlname="txtDescription" runat="server" /></td>
        <td valign="top" style="width:575px"><asp:TextBox ID="txtDescription" runat="server" width="300" CssClass="NormalTextBox" TextMode="MultiLine" Rows="5"></asp:TextBox></td>
    </tr>
    <tr>
        <td class="SubHead" style="width:175px" valign="top"><dnn:label id="plAddPage" controlname="chkAddPage" runat="server" /></td>
        <td valign="top" style="width:575px"><asp:CheckBox ID="chkAddPage" runat="server" /></td>
    </tr>
</table>
<p>
    <dnn:commandbutton id="cmdCreate" resourcekey="cmdCreate" runat="server" cssclass="CommandButton" ImageUrl="~/images/save.gif"/>&nbsp;&nbsp;
    <dnn:commandbutton id="cmdCancel" resourcekey="cmdCancel" runat="server" cssclass="CommandButton" ImageUrl="~/images/lt.gif" causesvalidation="False" />
</p>
<table class="Settings" cellspacing="2" cellpadding="2" summary="Packages Install Design Table">
    <tr><td><asp:PlaceHolder ID="phInstallLogs" runat="server" /></td></tr>
</table>
