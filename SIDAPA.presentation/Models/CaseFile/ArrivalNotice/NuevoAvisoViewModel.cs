using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.presentation.Models.CaseFile.ArrivalNotice;

/// <summary>Formulario "Nuevo aviso de arribo" (HU-02).</summary>
public class NuevoAvisoViewModel
{
    /// <summary>Renglones de archivo del formulario (5 × 20 MB cabe en el límite de la petición).</summary>
    public const int MaxDocumentos = 5;

    [Display(Name = "Tipo de operación")]
    public OperationTypeEnum TipoOperacion { get; set; } = OperationTypeEnum.IMPORTACION;

    [Required(ErrorMessage = "Indica la fecha estimada de arribo.")]
    [Display(Name = "Fecha estimada de arribo")]
    public DateOnly? FechaArribo { get; set; }

    [Required(ErrorMessage = "Elige la aduana de arribo.")]
    [Display(Name = "Aduana de arribo")]
    public string? Aduana { get; set; }

    [StringLength(ArrivalNoticeRule.MaxNotesLength, ErrorMessage = "Las observaciones no pueden pasar de {1} caracteres.")]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    public List<DocumentoItemViewModel> Documentos { get; set; } = [];

    // Catálogos: los llena el controller, no vienen en el POST.
    [BindNever] public IReadOnlyList<AduanaOptionDto> Aduanas { get; private set; } = [];
    [BindNever] public IReadOnlyList<DocumentTypeOptionDto> TiposDocumento { get; private set; } = [];

    /// <summary>Formulario vacío con la factura y el B/L ya sugeridos en los dos primeros renglones.</summary>
    public static NuevoAvisoViewModel Vacio() => new()
    {
        Documentos =
        [
            new DocumentoItemViewModel { Tipo = DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR },
            new DocumentoItemViewModel { Tipo = DocumentTypeEnum.DOCUMENTO_TRANSPORTE }
        ]
    };

    /// <summary>Pone los catálogos y deja siempre <see cref="MaxDocumentos"/> renglones.</summary>
    public NuevoAvisoViewModel Completar(ArrivalNoticeCatalogsDto catalogos)
    {
        Aduanas = catalogos.Aduanas;
        TiposDocumento = catalogos.DocumentTypes;

        if (Documentos.Count > MaxDocumentos)
            Documentos = Documentos.Take(MaxDocumentos).ToList();
        while (Documentos.Count < MaxDocumentos)
            Documentos.Add(new DocumentoItemViewModel());

        return this;
    }

    /// <summary>Renglones en los que el cliente eligió un archivo.</summary>
    public IEnumerable<DocumentoItemViewModel> ConArchivo() =>
        Documentos.Where(d => d.Archivo is not null);   // Completar ya dejó a lo más MaxDocumentos
}

public class DocumentoItemViewModel
{
    public DocumentTypeEnum? Tipo { get; set; }
    public IFormFile? Archivo { get; set; }
}

/// <summary>Modelo del partial de un renglón de documento (los nombres llevan el índice para el model binding).</summary>
public record RenglonDocumentoViewModel(int Indice, DocumentoItemViewModel Item, IReadOnlyList<DocumentTypeOptionDto> Tipos);
