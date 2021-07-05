using System.Text;

namespace Speedway.Core.MermaidJs
{
    public record MermaidConnector(string From, string To, MermaidConnectorType Type, string LinkText = "")
    {
        public void Generate(StringBuilder stringBuilder, int index)
        {
            var from = From.Replace("-", "_");
            var to = To.Replace("-", "_");
            var linkText = LinkText.Replace("-", "_");

            switch (Type)
            {
                case MermaidConnectorType.UsesCoreInfrastructure:
                    stringBuilder.AppendLine(
                        $"      {from}-->|{(string.IsNullOrWhiteSpace(linkText) ? Type.ToString() : linkText)}|{to}");
                    stringBuilder.AppendLine(
                        $"   linkStyle {index} stroke:green");
                    break;
                case MermaidConnectorType.AuthorisedBy:
                    stringBuilder.AppendLine(
                        $"      {from}-->|{(string.IsNullOrWhiteSpace(linkText) ? Type.ToString() : linkText)}|{to}");
                    break;
                case MermaidConnectorType.Authorises:
                    stringBuilder.AppendLine(
                        $"      {from} -. {(string.IsNullOrWhiteSpace(linkText) ? Type.ToString() : linkText)} .-> {to}");
                    stringBuilder.AppendLine(
                        $"   linkStyle {index} stroke:orange,color:orange,stroke-dasharray:5");
                    break;
            }
        }
    }
}