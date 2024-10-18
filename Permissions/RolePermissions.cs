namespace ToDoList.Authorization
{
    public static class RolePermissions
    {
        public const string Admin = "admin";
        public const string User = "user";

        public static Dictionary<string, string[]> Permissions = new Dictionary<string, string[]>
        {
            { Admin, new[] { "Create", "Edit", "Delete", "View" } },
            { User, new[] { "Create", "Edit", "View" } }
        };

        public static bool HasPermission(string role, string permission)
        {
            if (Permissions.TryGetValue(role, out var rolePermissions))
            {
                return rolePermissions.Contains(permission);
            }

            return false;
        }
    }
}
