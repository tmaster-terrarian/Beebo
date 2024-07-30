// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Sometimes I just dont feel like it.")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "I like being able to modify the x and y values directly.", Scope = "member", Target = "~F:BloodyCloth.Entity.drawScale")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "I like being able to modify the x and y values directly.", Scope = "member", Target = "~F:BloodyCloth.Entity.position")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "I like being able to modify the x and y values directly.", Scope = "member", Target = "~F:BloodyCloth.Entity.velocity")]
[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "It can't be helped with the current project structure.", Scope = "namespace", Target = "~N:Microsoft.Xna.Framework")]
[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "It can't be helped with the current project structure.", Scope = "namespace", Target = "~N:Microsoft.Xna.Framework.Input")]
