namespace SIDAPA.application.DTOs.Core.Auth;

/// <summary>Motivo por el que no se pudo iniciar sesión.</summary>
public enum LoginErrorEnum
{
    /// <summary>Correo inexistente, contraseña incorrecta o usuario inactivo (no se distingue a propósito).</summary>
    InvalidCredentials = 1
}
