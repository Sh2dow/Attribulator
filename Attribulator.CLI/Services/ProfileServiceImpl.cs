using System;
using System.Collections.Generic;
using Attribulator.API;
using Attribulator.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.CLI.Services
{
    public class ProfileServiceImpl : IProfileService
    {
        private readonly List<IProfile> _profiles = new List<IProfile>();
        private readonly IServiceProvider _serviceProvider;

        public ProfileServiceImpl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void RegisterProfile<TProfile>() where TProfile : IProfile
        {
            RegisterProfile(typeof(TProfile));
        }

        public void RegisterProfile(Type profileType)
        {
            _profiles.Add((IProfile) _serviceProvider.GetRequiredService(profileType));
        }

        public IEnumerable<IProfile> GetProfiles()
        {
            return _profiles;
        }

        public IProfile GetProfile(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                throw new KeyNotFoundException("Cannot find profile: (empty)");

            foreach (var profile in _profiles)
                if (string.Equals(profile.GetProfileId(), profileId, StringComparison.OrdinalIgnoreCase))
                    return profile;

            var normalizedInput = NormalizeProfileId(profileId);
            foreach (var profile in _profiles)
                if (NormalizeProfileId(profile.GetProfileId()) == normalizedInput)
                    return profile;

            throw new KeyNotFoundException($"Cannot find profile: {profileId}");
        }

        private static string NormalizeProfileId(string profileId)
        {
            var buffer = new char[profileId.Length];
            var count = 0;
            foreach (var ch in profileId.Trim().ToUpperInvariant())
            {
                if (ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                    buffer[count++] = ch;
            }

            return new string(buffer, 0, count);
        }
    }
}
