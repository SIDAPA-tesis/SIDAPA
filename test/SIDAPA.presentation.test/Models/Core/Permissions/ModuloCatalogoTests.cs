using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Models.Core.Permissions;
using static SIDAPA.domain.Enums.Core.ModuleEnum;

namespace SIDAPA.presentation.test.Models.Core.Permissions;

/// <summary>
/// <see cref="ModuloCatalogo"/> y los ViewModels que arma: el menú lateral y las tarjetas de accesos rápidos
/// salen de la misma lista de módulos por rol (IPermisosService).
/// </summary>
public class ModuloCatalogoTests
{
    public static TheoryData<ModuleEnum> TodosLosModulos() => new(Enum.GetValues<ModuleEnum>());

    [Theory]
    [MemberData(nameof(TodosLosModulos))]
    public void Get_CadaModulo_TieneTextoControllerYDescripcion(ModuleEnum modulo)
    {
        var vm = ModuloCatalogo.Get(modulo);

        vm.Modulo.ShouldBe(modulo);
        vm.Texto.ShouldNotBeNullOrWhiteSpace();
        vm.Controlador.ShouldNotBeNullOrWhiteSpace();
        vm.Descripcion.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Para_VariosModulos_ConservaElOrden()
    {
        var vms = ModuloCatalogo.Para([Usuarios, Inicio, Claves]);

        vms.Select(m => m.Modulo).ShouldBe([Usuarios, Inicio, Claves]);
    }

    [Fact]
    public void MenuLateral_ControllerActual_MarcaSoloEsaOpcionComoActiva()
    {
        var vm = MenuLateralViewModel.Desde("Ana", [nameof(RolesEnum.GLOSADOR)], [Inicio, Pedimentos, Claves], "claves");

        vm.Opciones.Where(o => o.Activa).ShouldHaveSingleItem().Controlador.ShouldBe("Claves");
    }

    [Fact]
    public void MenuLateral_VariosRoles_LosMuestraSeparadosPorComa()
    {
        var vm = MenuLateralViewModel.Desde("Ana", [nameof(RolesEnum.GLOSADOR), nameof(RolesEnum.CAPTURADOR)], [Inicio], null);

        vm.Rol.ShouldBe("GLOSADOR, CAPTURADOR");
        vm.Opciones.ShouldAllBe(o => !o.Activa);
    }

    [Fact]
    public void AccesosRapidos_NoIncluyeInicio()
    {
        var vm = AccesosRapidosViewModel.Desde([Inicio, Pedimentos, Claves]);

        vm.Accesos.Select(a => a.Modulo).ShouldBe([Pedimentos, Claves]);
    }

    [Fact]
    public void AccesosRapidos_RolSinModulos_QuedaVacio()
    {
        var vm = AccesosRapidosViewModel.Desde([Inicio]);   // VALIDADOR hoy

        vm.Accesos.ShouldBeEmpty();
    }
}
