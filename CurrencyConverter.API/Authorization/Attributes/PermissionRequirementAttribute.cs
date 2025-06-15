namespace CurrencyConverter.API.Authorization.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PermissionRequirementAttribute : Attribute
    {
        public string Resource { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string PermissionKey => $"{Resource}.{Action}";
    }
}
