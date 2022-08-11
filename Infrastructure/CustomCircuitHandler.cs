using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WhatToEatApp.Infrastructure
{
    public class CustomCircuitHandler : CircuitHandler
    {
        private readonly HashSet<Circuit> _circuitList;

        public CustomCircuitHandler()
        {
            _circuitList = new();
        }
        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            return base.OnConnectionUpAsync(circuit, cancellationToken);
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            return base.OnConnectionDownAsync(circuit, cancellationToken);
        }
    }
}