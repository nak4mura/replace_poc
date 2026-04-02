<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="EditScript.ascx.vb" Inherits="DotNetNuke.Modules.RazorHost.EditScript" %>
<%@ Register Assembly="DotnetNuke" Namespace="DotNetNuke.UI.WebControls" TagPrefix="dnn" %>
<%@ Register Assembly="DotnetNuke.Web" Namespace="DotNetNuke.Web.UI.WebControls" TagPrefix="dnn" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<dnn:DnnToolTipManager ID="toolTipManager" runat="server" Position="Center" RelativeTo="BrowserWindow"
    Width="500px" Height="200px" HideEvent="ManualClose" ShowEvent="OnClick" Modal="true"
    Skin="Default" RenderInPageRoot="true" AnimationDuration="200" ManualClose="true"
    ManualCloseButtonText="Close" />
<table>
    <tr>
        <td style="width:150px;" class="SubHead"><dnn:Label id="scriptsLabel" runat="Server" controlname="scriptList" /></td>
        <td style="width:600px;" class="NormalTextBox">
            <asp:DropDownList ID="scriptList" runat="server" AutoPostBack="true" />
            &nbsp;&nbsp;&nbsp;
            <dnn:CommandButton ID="addNewButton" runat="server" ResourceKey="AddNew" ImageUrl="~/images/add.gif" />
        </td>
    </tr>
    <tr style="height:10px"><td>&nbsp;</td></tr>
    <tr>
        <td colspan="2"><asp:Label ID="lblSourceFile" runat="server" cssClass="NormalBold"/></td>
    </tr>
    <tr style="height:10px"><td>&nbsp;</td></tr>
    <tr id="trSource" runat="server">
        <td colspan="2">
            <dnn:label id="plSource" controlname="txtSource" runat="server" cssClass="SubHead"/>
            <asp:TextBox ID="txtSource" runat="server" cssClass="NormalTextBox" TextMode="MultiLine" Rows="20" Columns="80" style="width:750px" />
        </td>
    </tr>
    <tr style="height:10px"><td>&nbsp;</td></tr>
    <tr>
        <td style="width:150px;" class="SubHead"><dnn:Label id="currentScriptLabel" runat="Server" controlname="isCurrentScript" /></td>
        <td style="width:600px;" class="NormalTextBox">
            <asp:CheckBox ID="isCurrentScript" runat="server" />
        </td>
    </tr>
    <tr style="height:10px"><td>&nbsp;</td></tr>
</table>
<p>
    <dnn:commandbutton id="cmdSave" resourcekey="cmdSave" runat="server" cssclass="CommandButton" ImageUrl="~/images/save.gif"/>&nbsp;&nbsp;
    <dnn:commandbutton id="cmdSaveClose" resourcekey="cmdSaveClose" runat="server" cssclass="CommandButton" ImageUrl="~/images/save.gif"/>&nbsp;&nbsp;
    <dnn:commandbutton id="cmdCancel" resourcekey="cmdCancel" runat="server" cssclass="CommandButton" ImageUrl="~/images/lt.gif" causesvalidation="False" />
</p>
