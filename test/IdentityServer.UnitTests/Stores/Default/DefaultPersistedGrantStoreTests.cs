﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using FluentAssertions;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.InMemory;
using IdentityServer4.Stores.Serialization;
using IdentityServer4.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace IdentityServer4.UnitTests.Stores.Default
{
    public class DefaultPersistedGrantStoreTests
    {
        InMemoryPersistedGrantStore _store = new InMemoryPersistedGrantStore();
        IAuthorizationCodeStore _codes;
        IRefreshTokenStore _refreshTokens;
        IReferenceTokenStore _referenceTokens;
        IUserConsentStore _userConsent;
        StubHandleGenerationService _stubHandleGenerationService = new StubHandleGenerationService();

        ClaimsPrincipal _user = IdentityServerPrincipal.Create("123", "bob");

        public DefaultPersistedGrantStoreTests()
        {
            _codes = new DefaultAuthorizationCodeStore(_store,
                new PersistentGrantSerializer(),
                _stubHandleGenerationService,
                TestLogger.Create<DefaultAuthorizationCodeStore>());
            _refreshTokens = new DefaultRefreshTokenStore(_store,
                new PersistentGrantSerializer(),
                _stubHandleGenerationService,
                TestLogger.Create<DefaultRefreshTokenStore>());
            _referenceTokens = new DefaultReferenceTokenStore(_store,
                new PersistentGrantSerializer(),
                _stubHandleGenerationService,
                TestLogger.Create<DefaultReferenceTokenStore>());
            _userConsent = new DefaultUserConsentStore(_store,
                new PersistentGrantSerializer(),
                _stubHandleGenerationService,
                TestLogger.Create<DefaultUserConsentStore>());
        }

        [Fact]
        public async Task StoreAuthorizationCodeAsync_should_persist_grant()
        {
            var code1 = new AuthorizationCode()
            {
                ClientId = "test",
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "scope1", "scope2" }
            };

            var handle = await _codes.StoreAuthorizationCodeAsync(code1);
            var code2 = await _codes.GetAuthorizationCodeAsync(handle);

            code1.ClientId.Should().Be(code2.ClientId);
            code1.CreationTime.Should().Be(code2.CreationTime);
            code1.Lifetime.Should().Be(code2.Lifetime);
            code1.Subject.GetSubjectId().Should().Be(code2.Subject.GetSubjectId());
            code1.CodeChallenge.Should().Be(code2.CodeChallenge);
            code1.RedirectUri.Should().Be(code2.RedirectUri);
            code1.Nonce.Should().Be(code2.Nonce);
            code1.RequestedScopes.ShouldBeEquivalentTo(code2.RequestedScopes);
        }

        [Fact]
        public async Task RemoveAuthorizationCodeAsync_should_remove_grant()
        {
            var code1 = new AuthorizationCode()
            {
                ClientId = "test",
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "scope1", "scope2" }
            };

            var handle = await _codes.StoreAuthorizationCodeAsync(code1);
            await _codes.RemoveAuthorizationCodeAsync(handle);
            var code2 = await _codes.GetAuthorizationCodeAsync(handle);
            code2.Should().BeNull();
        }

        [Fact]
        public async Task expired_code_should_not_load()
        {
            var code1 = new AuthorizationCode()
            {
                ClientId = "test",
                CreationTime = DateTime.UtcNow.AddHours(-1),
                Lifetime = 10,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "scope1", "scope2" }
            };
            var handle = await _codes.StoreAuthorizationCodeAsync(code1);
            
            var code2 = await _codes.GetAuthorizationCodeAsync(handle);
            code2.Should().BeNull();
        }

        [Fact]
        public async Task StoreRefreshTokenAsync_should_persist_grant()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };

            var handle = await _refreshTokens.StoreRefreshTokenAsync(token1);
            var token2 = await _refreshTokens.GetRefreshTokenAsync(handle);

            token1.ClientId.Should().Be(token2.ClientId);
            token1.CreationTime.Should().Be(token2.CreationTime);
            token1.Lifetime.Should().Be(token2.Lifetime);
            token1.Subject.GetSubjectId().Should().Be(token2.Subject.GetSubjectId());
            token1.Version.Should().Be(token2.Version);
            token1.AccessToken.Audiences.Count.Should().Be(1);
            token1.AccessToken.Audiences.First().Should().Be("aud");
            token1.AccessToken.ClientId.Should().Be(token2.AccessToken.ClientId);
            token1.AccessToken.CreationTime.Should().Be(token2.AccessToken.CreationTime);
            token1.AccessToken.Type.Should().Be(token2.AccessToken.Type);
        }

        [Fact]
        public async Task RemoveRefreshTokenAsync_should_remove_grant()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };


            var handle = await _refreshTokens.StoreRefreshTokenAsync(token1);
            await _refreshTokens.RemoveRefreshTokenAsync(handle);
            var token2 = await _refreshTokens.GetRefreshTokenAsync(handle);
            token2.Should().BeNull();
        }

        [Fact]
        public async Task expired_refresh_token_should_not_load()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow.AddHours(-1),
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };

            var handle = await _refreshTokens.StoreRefreshTokenAsync(token1);
            var token2 = await _refreshTokens.GetRefreshTokenAsync(handle);

            token2.Should().BeNull();
        }

        [Fact]
        public async Task RemoveRefreshTokenAsync_by_sub_and_client_should_remove_grant()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };

            var handle1 = await _refreshTokens.StoreRefreshTokenAsync(token1);
            var handle2 = await _refreshTokens.StoreRefreshTokenAsync(token1);
            await _refreshTokens.RemoveRefreshTokensAsync("123", "client");

            var token2 = await _refreshTokens.GetRefreshTokenAsync(handle1);
            token2.Should().BeNull();
            token2 = await _refreshTokens.GetRefreshTokenAsync(handle2);
            token2.Should().BeNull();
        }

        [Fact]
        public async Task StoreReferenceTokenAsync_should_persist_grant()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "foo")
                },
                Version = 1
            };

            var handle = await _referenceTokens.StoreReferenceTokenAsync(token1);
            var token2 = await _referenceTokens.GetReferenceTokenAsync(handle);

            token1.ClientId.Should().Be(token2.ClientId);
            token1.Audiences.Count.Should().Be(1);
            token1.Audiences.First().Should().Be("aud");
            token1.CreationTime.Should().Be(token2.CreationTime);
            token1.Type.Should().Be(token2.Type);
            token1.Lifetime.Should().Be(token2.Lifetime);
            token1.Version.Should().Be(token2.Version);
        }

        [Fact]
        public async Task RemoveReferenceTokenAsync_should_remove_grant()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "foo")
                },
                Version = 1
            };

            var handle = await _referenceTokens.StoreReferenceTokenAsync(token1);
            await _referenceTokens.RemoveReferenceTokenAsync(handle);
            var token2 = await _referenceTokens.GetReferenceTokenAsync(handle);
            token2.Should().BeNull();
        }

        [Fact]
        public async Task expired_reference_token_should_not_load()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow.AddHours(-1),
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "foo")
                },
                Version = 1
            };

            var handle = await _referenceTokens.StoreReferenceTokenAsync(token1);

            var token2 = await _referenceTokens.GetReferenceTokenAsync(handle);
            token2.Should().BeNull();
        }

        [Fact]
        public async Task RemoveReferenceTokenAsync_by_sub_and_client_should_remove_grant()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "foo")
                },
                Version = 1
            };

            var handle1 = await _referenceTokens.StoreReferenceTokenAsync(token1);
            var handle2 = await _referenceTokens.StoreReferenceTokenAsync(token1);
            await _referenceTokens.RemoveReferenceTokensAsync("123", "client");

            var token2 = await _referenceTokens.GetReferenceTokenAsync(handle1);
            token2.Should().BeNull();
            token2 = await _referenceTokens.GetReferenceTokenAsync(handle2);
            token2.Should().BeNull();
        }

        [Fact]
        public async Task StoreUserConsentAsync_should_persist_grant()
        {
            var consent1 = new Consent()
            {
                ClientId = "client",
                SubjectId = "123",
                Scopes = new string[] { "foo", "bar" }
            };

            await _userConsent.StoreUserConsentAsync(consent1);
            var consent2 = await _userConsent.GetUserConsentAsync("123", "client");

            consent2.ClientId.Should().Be(consent1.ClientId);
            consent2.SubjectId.Should().Be(consent1.SubjectId);
            consent2.Scopes.ShouldBeEquivalentTo(new string[] { "bar", "foo" });
        }

        [Fact]
        public async Task RemoveUserConsentAsync_should_remove_grant()
        {
            var consent1 = new Consent()
            {
                ClientId = "client",
                SubjectId = "123",
                Scopes = new string[] { "foo", "bar" }
            };

            await _userConsent.StoreUserConsentAsync(consent1);
            await _userConsent.RemoveUserConsentAsync("123", "client");
            var consent2 = await _userConsent.GetUserConsentAsync("123", "client");
            consent2.Should().BeNull();
        }

        [Fact]
        public async Task expired_user_consent_should_not_load()
        {
            var consent1 = new Consent()
            {
                ClientId = "client",
                SubjectId = "123",
                Scopes = new string[] { "foo", "bar" },
                CreationTime = DateTime.UtcNow.AddHours(-1),
                Expiration = DateTime.UtcNow.AddSeconds(-1)
            };

            await _userConsent.StoreUserConsentAsync(consent1);

            var consent2 = await _userConsent.GetUserConsentAsync("123", "client");
            consent2.Should().BeNull();
        }

        [Fact]
        public async Task same_key_for_different_grant_types_should_not_interfere_with_each_other()
        {
            _stubHandleGenerationService.Handle = "key";

            await _referenceTokens.StoreReferenceTokenAsync(new Token()
            {
                ClientId = "client1",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "bar1"),
                    new Claim("scope", "bar2"),
                },
            });

            await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 20,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "baz1"),
                        new Claim("scope", "baz2")
                    }
                },
                Version = 1
            });

            await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
            {
                ClientId = "client1",
                CreationTime = DateTime.UtcNow,
                Lifetime = 30,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "quux1", "quux2" }
            });

            (await _codes.GetAuthorizationCodeAsync("key")).Lifetime.Should().Be(30);
            (await _refreshTokens.GetRefreshTokenAsync("key")).Lifetime.Should().Be(20);
            (await _referenceTokens.GetReferenceTokenAsync("key")).Lifetime.Should().Be(10);
        }
    }
}
