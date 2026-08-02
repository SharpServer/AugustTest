using PlayerRoles;
using Sliced.API.Features;

namespace Sliced.API.Structs;

public record struct SpawnSetRoleDefinition(RoleTypeId VanillaRole, CustomRole CustomRole, int RoleAllowedCount, bool IsForced = false);