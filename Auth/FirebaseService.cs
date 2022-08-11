using System.Security.Principal;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace WhatToEatApp.Auth
{
    public class User : IIdentity
    {
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhotoUrl { get; set; }
        public string ProviderId { get; set; } = null!;
        public string Uid { get; set; } = null!;

        public string? AuthenticationType => "Firebase";

        public bool IsAuthenticated => true;

        public string? Name => DisplayName ?? Email;
    }
    public class FirebaseOptions
    {
        public string? ApiKey { get; set; }
        public string? AuthDomain { get; set; }
        public string? ProjectId { get; set; }
        public string? StorageBucket { get; set; }
        public string? MessagingSenderId { get; set; }
        public string? AppId { get; set; }
    }

    public interface IFirebaseService
    {
        Task InitializeFirebase();
        Task Authenticate(string email, string password);
        Task SignOut();
        Task<bool> IsUserSignedIn();
        event EventHandler<User?> SetLoggedInUserEvent;
        event EventHandler? SetLoggedOutUserEvent;
    }

    public class FirebaseService : IFirebaseService
    {
        public event EventHandler<User?>? SetLoggedInUserEvent;
        public event EventHandler? SetLoggedOutUserEvent;
        private readonly IJSRuntime _js;
        private IJSObjectReference? _jsModule;
        private User? _currentlyLoggedInUser;
        private readonly IOptions<FirebaseOptions> _firebaseOptions;
        private readonly JsonSerializerOptions _serializerOptions;

        public FirebaseService(IJSRuntime js, IOptions<FirebaseOptions> options)
        {
            _js = js;
            _firebaseOptions = options;
            _serializerOptions = new JsonSerializerOptions();
            _serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        }

        public async Task Authenticate(string email, string password)
        {
            try
            {
                _currentlyLoggedInUser = await _jsModule!.InvokeAsync<User?>("signIn", email, password);
                OnLoggedInUserSet(_currentlyLoggedInUser);
            }
            catch (JSException)
            {
                throw;
            }
        }

        public async Task SignOut()
        {
            await _jsModule!.InvokeVoidAsync("logout");
            OnLoggedOutUserSet();
        }

        public async Task InitializeFirebase()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_firebaseOptions.Value, _serializerOptions);
            _jsModule = await _js.InvokeAsync<IJSObjectReference>("import", "/js/firebaseAuth.js");
            _currentlyLoggedInUser = await _jsModule.InvokeAsync<User?>("initializeFirebase", json);
            OnLoggedInUserSet(_currentlyLoggedInUser);
        }

        public async Task<bool> IsUserSignedIn()
        {
            _jsModule = await _js.InvokeAsync<IJSObjectReference>("import", "/js/firebaseAuth.js");
            _currentlyLoggedInUser = await _jsModule!.InvokeAsync<User>("getCurrentlyLoggedOnUser");
            OnLoggedInUserSet(_currentlyLoggedInUser);
            return _currentlyLoggedInUser != null;
        }

        protected virtual void OnLoggedInUserSet(User? e)
        {
            EventHandler<User?>? handler = SetLoggedInUserEvent;
            handler?.Invoke(this, e);
        }

        protected virtual void OnLoggedOutUserSet()
        {
            EventHandler? handler = SetLoggedOutUserEvent;
            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}