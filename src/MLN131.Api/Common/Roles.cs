namespace MLN131.Api.Common;

public static class Roles
{
    public const string Admin = "admin";
    public const string User = "user";
    public const string Viewer = "viewer";

    public static readonly string[] All = [Admin, User, Viewer];
}

