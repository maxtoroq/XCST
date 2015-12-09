<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="Xcst.Web.Mvc.Html" %>

<script runat="server">

   void Application_Start(object sender, EventArgs e) {

      EditorExtensions.EditorCssClassFunction = (info, defaultClass) =>
         (info.InputType == InputType.Text
            || info.InputType == InputType.Password
            || info.TagName != "input") ? "form-control"
            : null;

      EditorExtensions.OmitPasswordValue = true;
   }

</script>
