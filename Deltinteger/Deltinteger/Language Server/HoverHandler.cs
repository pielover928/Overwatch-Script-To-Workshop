using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using IHoverHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.IHoverHandler;
using DefinitionCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.HoverCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    public class HoverHandler : IHoverHandler
    {
        private readonly DeltintegerLanguageServer _languageServer;

        public HoverHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public TextDocumentRegistrationOptions GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions() {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector
            };
        }

        public async Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            var hoverRanges = _languageServer.LastParse?.ScriptFromUri(request.TextDocument.Uri)?.GetHoverRanges();
            if (hoverRanges == null || hoverRanges.Length == 0) return new Hover();

            HoverRange chosen = hoverRanges
                .Where(hoverRange => hoverRange.Range.IsInside(request.Position))
                .OrderBy(hoverRange => hoverRange.Range)
                .FirstOrDefault();
            
            if (chosen == null) return new Hover();

            return new Hover() {
                Range = chosen.Range.ToLsRange(),
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent() {
                    Kind = MarkupKind.Markdown,
                    Value = chosen.Content
                })
            };
        }

        // Definition capability
        private DefinitionCapability _capability;
        public void SetCapability(DefinitionCapability capability)
        {
            _capability = capability;
        }

        public static string GetLabel(string name, CodeParameter[] parameters, bool markdown, StringOrMarkupContent description)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            string result = "";
            if (markdown) result += "```ostw\n";
            result += name + CodeParameter.GetLabels(parameters, false);
            if (markdown) result += "\n\r```";
            if (markdown && description != null) result += "\n\r ----- \n\r" + (description.HasString ? description.String : description.MarkupContent.Value);
            return result;
        }

        public static string Sectioned(string title, string description)
        {
            string result = "";
            result += "```ostw\n";
            result += title;
            result += "\n\r```";
            if (description != null) result += "\n\r ----- \n\r" + description;
            return result;
        }
    }
}