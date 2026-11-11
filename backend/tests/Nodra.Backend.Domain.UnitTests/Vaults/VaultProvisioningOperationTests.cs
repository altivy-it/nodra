using Nodra.Backend.Domain.Vaults;
using Xunit;

namespace Nodra.Backend.Domain.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class VaultProvisioningOperationTests
{
    [Fact]
    public void ProvisioningOperationAllowsReadyTransition()
    {
        var operation = VaultProvisioningOperation.Create(
            Guid.NewGuid(), "vault-0001", "Pessoal", "subject-1");

        operation.TransitionTo(VaultLifecycleState.Provisioning);
        operation.TransitionTo(VaultLifecycleState.Ready);

        Assert.Equal(VaultLifecycleState.Ready, operation.State);
    }

    [Fact]
    public void ProvisioningOperationRejectsTransitionFromArchived()
    {
        var operation = VaultProvisioningOperation.Create(
            Guid.NewGuid(), "vault-0001", "Pessoal", "subject-1");
        operation.TransitionTo(VaultLifecycleState.Provisioning);
        operation.TransitionTo(VaultLifecycleState.Ready);
        operation.TransitionTo(VaultLifecycleState.Archived);

        _ = Assert.Throws<InvalidOperationException>(() =>
            operation.TransitionTo(VaultLifecycleState.Provisioning));
    }
}
