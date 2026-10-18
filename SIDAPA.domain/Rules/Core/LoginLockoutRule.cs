using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Rules.Core;

public static class LoginLockoutRule
{
    public const int MaxFailedAttemps = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static void EnsureNotLocked(UserEntity user, DateTime now)
    {
        if(user.LockedUntil is not {} until) return;

        if(until <= now)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            return;
        }

        throw new AccountLockedException(until - now);
    }

    public static void RegisterFailure(UserEntity user, DateTime now)
    {
        user.FailedLoginAttempts++;
        if(user.FailedLoginAttempts >= MaxFailedAttemps)
            user.LockedUntil = now + LockoutDuration;
    }

    public static void RegisterSuccess(UserEntity user)
    {
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
    }
}