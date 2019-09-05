using System;
using System.Linq;
using JetBrains.Annotations;
using WireMock.Owin;
using WireMock.Serialization;
using WireMock.Validation;

namespace WireMock.Matchers.Request
{
    /// <summary>
    /// The request verb matcher.
    /// </summary>
    internal class RequestMessageMappingGuidMatcher : IRequestMatcher
    {
        private readonly MatchBehaviour _matchBehaviour;
        private readonly IWireMockMiddlewareOptions _options = new WireMockMiddlewareOptions();

        private const string AdminMappings = "/__admin/mappings";

        /// <summary>
        /// The methods
        /// </summary>
        public Guid TargetMappingGuid { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMessageMappingGuidMatcher"/> class.
        /// </summary>
        /// <param name="matchBehaviour">The match behaviour.</param>
        /// <param name="methods">The methods.</param>
        public RequestMessageMappingGuidMatcher(MatchBehaviour matchBehaviour, [NotNull] Guid mappingGuid)
        {
            Check.NotNull(mappingGuid, nameof(mappingGuid));
            _matchBehaviour = matchBehaviour;

            TargetMappingGuid = mappingGuid;
        }

        /// <inheritdoc cref="IRequestMatcher.GetMatchingScore"/>
        public double GetMatchingScore(RequestMessage requestMessage, RequestMatchResult requestMatchResult)
        {
            double score = MatchBehaviourHelper.Convert(_matchBehaviour, IsMatch(requestMessage));
            return requestMatchResult.AddScore(GetType(), score);
        }

        private double IsMatch(RequestMessage requestMessage)
        {
            Guid guid = ParseGuidFromRequestMessage(requestMessage);
            var entry = _options.LogEntries.FirstOrDefault(r => !r.RequestMessage.Path.StartsWith("/__admin/") && r.Guid == guid);
            return MatchScores.ToScore(entry?.MappingGuid == TargetMappingGuid);
        }

        private Guid ParseGuidFromRequestMessage(RequestMessage requestMessage)
        {
            return Guid.Parse(requestMessage.Path.Substring(AdminMappings.Length + 1));
        }
    }
}