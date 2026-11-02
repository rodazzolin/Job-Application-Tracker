using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace JobTracker.Models;

public static class EnumExtensions
{
    // Returns the [Display(Name = ...)] label of an enum value, or its name if it has none.
    public static string GetDisplayName(this Enum value) =>
        value.GetType().GetMember(value.ToString()).FirstOrDefault()
            ?.GetCustomAttribute<DisplayAttribute>()?.GetName()
        ?? value.ToString();
}
