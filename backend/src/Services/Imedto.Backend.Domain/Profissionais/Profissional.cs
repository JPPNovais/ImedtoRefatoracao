using Imedto.Backend.Domain.Profissionais.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Profissionais;

/// <summary>
/// Aggregate root de Profissional. Relação 1:1 com <c>Usuario</c> — o Id é o mesmo UUID
/// do usuário (e, por consequência, do <c>auth.users</c> do Supabase).
/// </summary>
public class Profissional : Entity<Guid>
{
    public virtual string Conselho { get; protected set; }
    public virtual string Uf { get; protected set; }
    public virtual string NumeroRegistro { get; protected set; }
    public virtual string Especialidade { get; protected set; }
    public virtual string Bio { get; protected set; }
    public virtual string FotoUrl { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected Profissional() { }

    public static Profissional Cadastrar(
        Guid usuarioId,
        string conselho,
        string uf,
        string numeroRegistro,
        string especialidade,
        string bio)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário é obrigatório.");

        ValidarCamposConselho(conselho, uf, numeroRegistro);

        var prof = new Profissional
        {
            Id = usuarioId,
            Conselho = conselho.Trim().ToUpperInvariant(),
            Uf = uf.Trim().ToUpperInvariant(),
            NumeroRegistro = numeroRegistro.Trim(),
            Especialidade = string.IsNullOrWhiteSpace(especialidade) ? null : especialidade.Trim(),
            Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        prof.AddDomainEvent(new ProfissionalCadastradoEvent(
            prof.Id, prof.Conselho, prof.Uf, prof.NumeroRegistro));

        return prof;
    }

    public virtual void AlterarFoto(string fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl))
            throw new BusinessException("URL da foto é obrigatória.");

        FotoUrl = fotoUrl.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Atualizar(
        string conselho,
        string uf,
        string numeroRegistro,
        string especialidade,
        string bio)
    {
        ValidarCamposConselho(conselho, uf, numeroRegistro);

        Conselho = conselho.Trim().ToUpperInvariant();
        Uf = uf.Trim().ToUpperInvariant();
        NumeroRegistro = numeroRegistro.Trim();
        Especialidade = string.IsNullOrWhiteSpace(especialidade) ? null : especialidade.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void ValidarCamposConselho(string conselho, string uf, string numeroRegistro)
    {
        if (string.IsNullOrWhiteSpace(conselho))
            throw new BusinessException("Conselho profissional é obrigatório (ex.: CRM, CRO, CRF).");
        if (string.IsNullOrWhiteSpace(uf) || uf.Trim().Length != 2)
            throw new BusinessException("UF deve conter 2 letras.");
        if (string.IsNullOrWhiteSpace(numeroRegistro))
            throw new BusinessException("Número de registro no conselho é obrigatório.");
    }
}
