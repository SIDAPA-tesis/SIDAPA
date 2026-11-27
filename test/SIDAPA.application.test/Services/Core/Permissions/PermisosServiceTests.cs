using SIDAPA.application.Services.Core;
using SIDAPA.domain.Enums.Core;
using static SIDAPA.domain.Enums.Core.ModuleEnum;

namespace SIDAPA.application.test.Services.Core.Permissions;

/// <summary>
/// Pruebas de <see cref="PermisosService"/>: qué módulos ve cada rol en el menú lateral y en los accesos rápidos.
/// Debe coincidir con las políticas de docs/permisos-por-rol.md (SCRUM-42/43).
/// </summary>
public class PermisosServiceTests
{
    private readonly PermisosService _service = new();

    public static TheoryData<RolesEnum, ModuleEnum[]> ModulosPorRol() => new()
    {
        { RolesEnum.CAPTURADOR, [Inicio, Expedientes, Pedimentos] },
        { RolesEnum.GLOSADOR, [Inicio, Expedientes, Pedimentos, Claves, Productos] },
        { RolesEnum.ADMINISTRADOR, [Inicio, Expedientes, Pedimentos, Claves, Reportes, Productos, Usuarios] },
        { RolesEnum.VALIDADOR, [Inicio] },   // todavía sin política: solo Inicio
        { RolesEnum.CLIENTE, [Inicio, AvisosArribo] },
    };

    [Theory]
    [MemberData(nameof(ModulosPorRol))]
    public void ModulosPara_CadaRol_RegresaSusModulosEnElOrdenDelMenu(RolesEnum rol, ModuleEnum[] esperados)
    {
        var modulos = _service.ModulosPara([rol.ToString()]);

        modulos.ShouldBe(esperados);
    }

    [Fact]
    public void ModulosPara_Administrador_NoVeElPortalDelCliente()
    {
        var modulos = _service.ModulosPara([nameof(RolesEnum.ADMINISTRADOR)]);

        modulos.ShouldNotContain(AvisosArribo);
    }

    [Theory]
    [InlineData("glosador")]      // el rol del token se compara exacto, igual que en las políticas
    [InlineData("SUPERUSUARIO")]
    public void ModulosPara_RolQueNoExisteTalCual_SoloRegresaInicio(string rol)
    {
        var modulos = _service.ModulosPara([rol]);

        modulos.ShouldBe([Inicio]);
    }

    [Fact]
    public void ModulosPara_SinRoles_SoloRegresaInicio()
    {
        var modulos = _service.ModulosPara([]);

        modulos.ShouldBe([Inicio]);
    }

    [Fact]
    public void ModulosPara_VariosRoles_UneSusModulosSinRepetirYEnOrden()
    {
        var modulos = _service.ModulosPara([nameof(RolesEnum.CLIENTE), nameof(RolesEnum.CAPTURADOR)]);

        modulos.ShouldBe([Inicio, Expedientes, Pedimentos, AvisosArribo]);
    }
}
