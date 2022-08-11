using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace WhatToEatApp.Auth
{
    public class FirebaseAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IFirebaseService _firebaseService;

        public FirebaseAuthStateProvider(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            _firebaseService.SetLoggedInUserEvent += (sender, e) =>
            {
                if (e is not null)
                {
                    var principal = new ClaimsPrincipal(e);
                    var authenticatedUser = new AuthenticationState(principal);
                    NotifyAuthenticationStateChanged(Task.FromResult(authenticatedUser));
                }
            };
            _firebaseService.SetLoggedOutUserEvent += (sender, e) =>
            {
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>())))));
            };
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return await Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>()))));
        }

    }
}