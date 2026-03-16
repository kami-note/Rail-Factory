namespace RailFactory.IAM.Domain.User;

/// <summary>
/// Role for RBAC. Matrix Admin can see all tenants; Branch Admin and Operator are scoped to one tenant (doc 06 §3.1, RF-IA-04).
/// </summary>
public enum Role
{
    Operator = 0,
    BranchAdmin = 1,
    MatrixAdmin = 2
}
