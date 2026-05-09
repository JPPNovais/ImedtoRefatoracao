using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            // Extensions Postgres usadas no schema (idempotentes — IF NOT EXISTS).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");

            migrationBuilder.CreateTable(
                name: "ai_audit_logs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    prompt_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tokens_in = table.Column<int>(type: "integer", nullable: true),
                    tokens_out = table.Column<int>(type: "integer", nullable: true),
                    modelo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    duracao_ms = table.Column<int>(type: "integer", nullable: true),
                    sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    erro_mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: true),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: true),
                    evolucao_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_delete_attempts",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tabela = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    registro_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tentado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_delete_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_credenciais",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    email_confirmado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bloqueado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_bloqueio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tentativas_falhas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ultimo_login_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_credenciais", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "automation_rules",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    evento_gatilho = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    condicoes_json = table.Column<string>(type: "jsonb", nullable: false),
                    acoes_json = table.Column<string>(type: "jsonb", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalogo_procedimentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capitulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogo_procedimentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "estabelecimentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dono_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_fantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    razao_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    endereco = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    foto_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    horario_inicio = table.Column<TimeOnly>(type: "time", nullable: false, defaultValueSql: "'08:00'::time"),
                    horario_fim = table.Column<TimeOnly>(type: "time", nullable: false, defaultValueSql: "'18:00'::time"),
                    duracao_consulta_padrao_minutos = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    intervalo_entre_consultas_minutos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    dias_semana_funcionamento = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[1,2,3,4,5]'::jsonb"),
                    horarios_bloqueados = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    datas_bloqueadas = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estabelecimentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exame_fisico",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    evolucao_id = table.Column<long>(type: "bigint", nullable: false),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    realizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    realizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dados_gerais_json = table.Column<string>(type: "jsonb", nullable: true),
                    observacoes_gerais = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exame_fisico", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "public",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    hash_payload = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    response_json = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "jobs_agendados",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    proximo_run_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ultimo_run_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    intervalo_seg = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ultima_falha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs_agendados", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lgpd_anonimizacoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tabela = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    registro_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    anonimizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    executado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lgpd_anonimizacoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lgpd_consentimentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    versao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    aceito_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lgpd_consentimentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medicamentos_favoritos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    medicamento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posologia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    via_administracao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    uso_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ultimo_uso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicamentos_favoritos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modelo_de_prontuario",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estrutura = table.Column<string>(type: "jsonb", nullable: false),
                    eh_padrao_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modelo_de_prontuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modelo_permissao_estabelecimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_acesso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    permissoes = table.Column<string>(type: "jsonb", nullable: false),
                    permissoes_extras = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    eh_padrao = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    icone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modelo_permissao_estabelecimento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notificacoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    categoria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    link_acao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    lida = table.Column<bool>(type: "boolean", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificacoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "paciente_acesso_log",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_acesso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ocorrido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paciente_acesso_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome_completo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    documento_internacional = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    data_nascimento = table.Column<DateTime>(type: "date", nullable: true),
                    genero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    endereco = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "ARRAY[]::text[]"),
                    alertas = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "ARRAY[]::text[]"),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anonimizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    anonimizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    preco_mensal = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    limite_profissionais = table.Column<int>(type: "integer", nullable: true),
                    limite_pacientes = table.Column<int>(type: "integer", nullable: true),
                    features_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profissionais",
                schema: "public",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conselho = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uf = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    numero_registro = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    especialidade = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    foto_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profissionais", x => x.usuario_id);
                });

            migrationBuilder.CreateTable(
                name: "profissoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    conselho_sigla = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profissoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prontuario_acesso_log",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_acesso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ocorrido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuario_acesso_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prontuario_anexos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    evolucao_id = table.Column<long>(type: "bigint", nullable: true),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    nome_original = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    arquivado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    arquivado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuario_anexos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prontuario_evolucoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    autor_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    modelo_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    modelo_de_prontuario_id_origem = table.Column<long>(type: "bigint", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuario_evolucoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prontuario_variaveis_pool",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    eh_padrao_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuario_variaveis_pool", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prontuarios",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    modelo_de_prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "receitas",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    tipo_notificacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    emitida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    validade_ate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    requer_retencao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cancelada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receitas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "receitas_configuracao_estabelecimento",
                schema: "public",
                columns: table => new
                {
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    cabecalho_html = table.Column<string>(type: "text", nullable: true),
                    rodape_html = table.Column<string>(type: "text", nullable: true),
                    modelo_padrao_id = table.Column<long>(type: "bigint", nullable: true),
                    emissor_padrao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receitas_configuracao_estabelecimento", x => x.estabelecimento_id);
                });

            migrationBuilder.CreateTable(
                name: "regioes_anatomicas_catalogo",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    pai_codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    nivel = table.Column<short>(type: "smallint", nullable: false),
                    vista = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    template_texto = table.Column<string>(type: "text", nullable: true),
                    svg_coords = table.Column<string>(type: "jsonb", nullable: true),
                    ordem = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    lateralidade = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regioes_anatomicas_catalogo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "solicitacoes_vinculo",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    respondida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    respondida_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo_recusa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitacoes_vinculo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipo_sala_atendimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_sala_atendimento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    nome_completo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    onboarding_completo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultimo_acesso_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vinculo_profissional_estabelecimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    modelo_permissao_id = table.Column<long>(type: "bigint", nullable: true),
                    convidado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    convidado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    aceito_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inativado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    nome_convidado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    telefone_convidado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    especialidade_convidada = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vinculo_profissional_estabelecimento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_email_tokens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_email_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_auth_email_tokens_auth_credenciais_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "public",
                        principalTable: "auth_credenciais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_refresh_tokens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revogado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_auth_refresh_tokens_auth_credenciais_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "public",
                        principalTable: "auth_credenciais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "automation_events",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    regra_id = table.Column<long>(type: "bigint", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tentativa_n = table.Column<int>(type: "integer", nullable: false),
                    executar_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    executado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultima_falha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_automation_events_regra",
                        column: x => x.regra_id,
                        principalSchema: "public",
                        principalTable: "automation_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categorias_financeiras",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    padrao = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_financeiras", x => x.id);
                    table.ForeignKey(
                        name: "fk_categoria_financeira_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuracoes_automacao",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    lembretes_habilitados = table.Column<bool>(type: "boolean", nullable: false),
                    horas_antecedencia_lembrete = table.Column<int>(type: "integer", nullable: false),
                    expiracao_orcamentos_habilitada = table.Column<bool>(type: "boolean", nullable: false),
                    email_remetente = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes_automacao", x => x.id);
                    table.ForeignKey(
                        name: "fk_configuracao_automacao_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "establishment_ai_settings",
                schema: "public",
                columns: table => new
                {
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    ai_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ai_provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "anthropic"),
                    ai_model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "claude-sonnet-4-6"),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    rate_limit_per_day = table.Column<int>(type: "integer", nullable: false, defaultValue: 200),
                    data_minimization_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "standard"),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establishment_ai_settings", x => x.estabelecimento_id);
                    table.ForeignKey(
                        name: "FK_establishment_ai_settings_estabelecimentos_estabelecimento_~",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "formas_pagamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    padrao = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formas_pagamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_forma_pagamento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_inventario",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unidade_medida = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantidade_atual = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    quantidade_minima = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    custo_medio = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_inventario", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventario_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_cirurgia",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    valor_base = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    duracao_padrao_minutos = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_cirurgia", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_cirurgia_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_equipe",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    valor_padrao = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_equipe", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_equipe_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_produto",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    valor_referencia = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    uso_unico = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_produto", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_produto_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_configuracao_local_cirurgia",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_internacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tempo_base_minutos = table.Column<int>(type: "integer", nullable: false),
                    valor_base = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tempo_adicional_minutos = table.Column<int>(type: "integer", nullable: false),
                    valor_adicional = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_configuracao_local_cirurgia", x => x.id);
                    table.ForeignKey(
                        name: "fk_config_local_cirurgia_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_valor_profissional",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    funcao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    tempo_base_minutos = table.Column<int>(type: "integer", nullable: false),
                    valor_tempo_base = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tempo_adicional_minutos = table.Column<int>(type: "integer", nullable: false),
                    valor_adicional = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_plus = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_valor_profissional", x => x.id);
                    table.ForeignKey(
                        name: "fk_valor_prof_orc_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unidades_estabelecimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_principal = table.Column<bool>(type: "boolean", nullable: false),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidades_estabelecimento", x => x.id);
                    table.ForeignKey(
                        name: "fk_unidades_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exame_fisico_regioes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exame_fisico_id = table.Column<long>(type: "bigint", nullable: false),
                    regiao_codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    regiao_pai_codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    lateralidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    achados = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    severidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exame_fisico_regioes", x => x.id);
                    table.ForeignKey(
                        name: "FK_exame_fisico_regioes_exame_fisico_exame_fisico_id",
                        column: x => x.exame_fisico_id,
                        principalSchema: "public",
                        principalTable: "exame_fisico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agendamentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio_previsto = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fim_previsto = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_servico = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lembrete_por_email_enviado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agendamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_agendamento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agendamento_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assinaturas",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    plano_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    iniciada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    renovada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assinaturas", x => x.id);
                    table.ForeignKey(
                        name: "FK_assinaturas_estabelecimentos_estabelecimento_id",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assinaturas_planos_plano_id",
                        column: x => x.plano_id,
                        principalSchema: "public",
                        principalTable: "planos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "especialidades",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profissao_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_especialidades", x => x.id);
                    table.ForeignKey(
                        name: "fk_especialidades_profissao",
                        column: x => x.profissao_id,
                        principalSchema: "public",
                        principalTable: "profissoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receita_itens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    receita_id = table.Column<long>(type: "bigint", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    medicamento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posologia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    via_administracao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    concentracao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    forma_farmaceutica = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    duracao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receita_itens", x => x.id);
                    table.ForeignKey(
                        name: "FK_receita_itens_receitas_receita_id",
                        column: x => x.receita_id,
                        principalSchema: "public",
                        principalTable: "receitas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_configuracao_pagamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    forma_pagamento_id = table.Column<long>(type: "bigint", nullable: false),
                    acrescimo_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    entrada_percentual_padrao = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    taxa_parcela = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    parcelas_maximas = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_configuracao_pagamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_config_pgto_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_config_pgto_forma_pagamento",
                        column: x => x.forma_pagamento_id,
                        principalSchema: "public",
                        principalTable: "formas_pagamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimentacoes_estoque",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_inventario_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    quantidade_anterior = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    quantidade_apos = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    custo_unitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    custo_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimentacoes_estoque", x => x.id);
                    table.ForeignKey(
                        name: "fk_movimentacao_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimentacao_item_inventario",
                        column: x => x.item_inventario_id,
                        principalSchema: "public",
                        principalTable: "itens_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_implante",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    item_inventario_id = table.Column<long>(type: "bigint", nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    custo_unitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_implante", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalogo_implante_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalogo_implante_item_inventario",
                        column: x => x.item_inventario_id,
                        principalSchema: "public",
                        principalTable: "itens_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_catalogo_cirurgia_produto",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    catalogo_cirurgia_id = table.Column<long>(type: "bigint", nullable: false),
                    catalogo_produto_id = table.Column<long>(type: "bigint", nullable: false),
                    quantidade_padrao = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    obrigatorio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_catalogo_cirurgia_produto", x => x.id);
                    table.ForeignKey(
                        name: "fk_cirurgia_produto_cirurgia",
                        column: x => x.catalogo_cirurgia_id,
                        principalSchema: "public",
                        principalTable: "orcamento_catalogo_cirurgia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cirurgia_produto_produto",
                        column: x => x.catalogo_produto_id,
                        principalSchema: "public",
                        principalTable: "orcamento_catalogo_produto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sala_atendimento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    unidade_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_sala_id = table.Column<long>(type: "bigint", nullable: true),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sala_atendimento", x => x.id);
                    table.ForeignKey(
                        name: "fk_salas_estab",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_salas_tipo",
                        column: x => x.tipo_sala_id,
                        principalSchema: "public",
                        principalTable: "tipo_sala_atendimento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_salas_unidade",
                        column: x => x.unidade_id,
                        principalSchema: "public",
                        principalTable: "unidades_estabelecimento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lista_espera_agendamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    profissional_preferido_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prioridade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Rotina"),
                    preferencia_periodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Qualquer"),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atendido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atendido_por_agendamento_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lista_espera_agendamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_lista_espera_agendamento",
                        column: x => x.atendido_por_agendamento_id,
                        principalSchema: "public",
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lista_espera_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lista_espera_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procedimentos_cirurgicos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    prontuario_id = table.Column<long>(type: "bigint", nullable: false),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    agendamento_id = table.Column<long>(type: "bigint", nullable: true),
                    data_agendada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_realizada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cirurgia_principal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cirurgia_codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    descricao_cirurgica = table.Column<string>(type: "text", nullable: true),
                    ficha_anestesica = table.Column<string>(type: "jsonb", nullable: true),
                    evolucao_pos_op = table.Column<string>(type: "text", nullable: true),
                    observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cancelado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deletado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedimentos_cirurgicos", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedimento_agendamento",
                        column: x => x.agendamento_id,
                        principalSchema: "public",
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_procedimento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_procedimento_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_procedimento_prontuario",
                        column: x => x.prontuario_id,
                        principalSchema: "public",
                        principalTable: "prontuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipe_cirurgica",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    procedimento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    papel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipe_cirurgica", x => x.id);
                    table.ForeignKey(
                        name: "fk_membro_equipe_cirurgica_procedimento",
                        column: x => x.procedimento_id,
                        principalSchema: "public",
                        principalTable: "procedimentos_cirurgicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    validade = table.Column<DateOnly>(type: "date", nullable: false),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    procedimento_cirurgico_id = table.Column<long>(type: "bigint", nullable: true),
                    custo_implantes_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orcamento_paciente",
                        column: x => x.paciente_id,
                        principalSchema: "public",
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orcamento_procedimento_cirurgico",
                        column: x => x.procedimento_cirurgico_id,
                        principalSchema: "public",
                        principalTable: "procedimentos_cirurgicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "itens_orcamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    valor_unitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    desconto_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_orcamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_orcamento_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estabelecimento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    data_pagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: true),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_lancamento_estabelecimento",
                        column: x => x.estabelecimento_id,
                        principalSchema: "public",
                        principalTable: "estabelecimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lancamento_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_anestesia",
                schema: "public",
                columns: table => new
                {
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_anestesia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_anestesia", x => x.orcamento_id);
                    table.ForeignKey(
                        name: "fk_orcamento_anestesia_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_cirurgias",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    procedimento_cirurgico_id = table.Column<long>(type: "bigint", nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    duracao_minutos = table.Column<int>(type: "integer", nullable: true),
                    valor_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_cirurgias", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_cirurgia_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_orcamento_cirurgia_procedimento_cirurgico",
                        column: x => x.procedimento_cirurgico_id,
                        principalSchema: "public",
                        principalTable: "procedimentos_cirurgicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_equipe",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    profissional_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    papel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_equipe", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_equipe_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_formas_pagamento",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    forma_pagamento_id = table.Column<long>(type: "bigint", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    parcelas = table.Column<int>(type: "integer", nullable: false),
                    acrescimo_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    entrada_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_formas_pagamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_forma_pagamento_forma",
                        column: x => x.forma_pagamento_id,
                        principalSchema: "public",
                        principalTable: "formas_pagamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orcamento_forma_pagamento_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_implantes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    item_inventario_id = table.Column<long>(type: "bigint", nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    custo_unitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    custo_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_implantes", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamento_implante_item_inventario",
                        column: x => x.item_inventario_id,
                        principalSchema: "public",
                        principalTable: "itens_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_orcamento_implante_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_internacao",
                schema: "public",
                columns: table => new
                {
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_internacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dias = table.Column<int>(type: "integer", nullable: false),
                    valor_diaria = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_internacao", x => x.orcamento_id);
                    table.ForeignKey(
                        name: "fk_orcamento_internacao_orcamento",
                        column: x => x.orcamento_id,
                        principalSchema: "public",
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_estab_inicio",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "estabelecimento_id", "inicio_previsto" });

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_paciente_inicio",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "paciente_id", "inicio_previsto" });

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_prof_inicio",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "profissional_usuario_id", "inicio_previsto" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_audit_estab_data",
                schema: "public",
                table: "ai_audit_logs",
                columns: new[] { "estabelecimento_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ai_audit_usuario_data",
                schema: "public",
                table: "ai_audit_logs",
                columns: new[] { "usuario_id", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_assinaturas_plano_id",
                schema: "public",
                table: "assinaturas",
                column: "plano_id");

            migrationBuilder.CreateIndex(
                name: "ix_assinaturas_status_expira",
                schema: "public",
                table: "assinaturas",
                columns: new[] { "status", "expira_em" });

            migrationBuilder.CreateIndex(
                name: "uq_assinaturas_estabelecimento",
                schema: "public",
                table: "assinaturas",
                column: "estabelecimento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_delete_estab_data",
                schema: "public",
                table: "audit_delete_attempts",
                columns: new[] { "estabelecimento_id", "tentado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_delete_tabela_data",
                schema: "public",
                table: "audit_delete_attempts",
                columns: new[] { "tabela", "tentado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_auth_credenciais_email",
                schema: "public",
                table: "auth_credenciais",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_email_tokens_token_hash",
                schema: "public",
                table: "auth_email_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_token_usuario_tipo",
                schema: "public",
                table: "auth_email_tokens",
                columns: new[] { "usuario_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_auth_refresh_tokens_token_hash",
                schema: "public",
                table: "auth_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_expira",
                schema: "public",
                table: "auth_refresh_tokens",
                column: "expira_em");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_usuario_ativo",
                schema: "public",
                table: "auth_refresh_tokens",
                columns: new[] { "usuario_id", "revogado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_automation_events_regra_id",
                schema: "public",
                table: "automation_events",
                column: "regra_id");

            migrationBuilder.CreateIndex(
                name: "ix_automation_events_status_executar_em",
                schema: "public",
                table: "automation_events",
                columns: new[] { "status", "executar_em" });

            migrationBuilder.CreateIndex(
                name: "ix_automation_rules_estab_evento_ativa",
                schema: "public",
                table: "automation_rules",
                columns: new[] { "estabelecimento_id", "evento_gatilho", "ativa" });

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_procedimentos_ativo_origem",
                schema: "public",
                table: "catalogo_procedimentos",
                columns: new[] { "ativo", "origem" });

            migrationBuilder.CreateIndex(
                name: "uq_catalogo_procedimentos_codigo",
                schema: "public",
                table: "catalogo_procedimentos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categoria_financeira_estab_tipo_ativo",
                schema: "public",
                table: "categorias_financeiras",
                columns: new[] { "estabelecimento_id", "tipo", "ativo" });

            migrationBuilder.CreateIndex(
                name: "uq_categoria_financeira_estab_nome",
                schema: "public",
                table: "categorias_financeiras",
                columns: new[] { "estabelecimento_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_configuracoes_automacao_estabelecimento",
                schema: "public",
                table: "configuracoes_automacao",
                column: "estabelecimento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_equipe_cirurgica_procedimento_papel",
                schema: "public",
                table: "equipe_cirurgica",
                columns: new[] { "procedimento_id", "papel" });

            migrationBuilder.CreateIndex(
                name: "uq_equipe_cirurgica_procedimento_profissional_papel",
                schema: "public",
                table: "equipe_cirurgica",
                columns: new[] { "procedimento_id", "profissional_usuario_id", "papel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_especialidades_profissao_ativo",
                schema: "public",
                table: "especialidades",
                columns: new[] { "profissao_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "uq_especialidades_profissao_nome",
                schema: "public",
                table: "especialidades",
                columns: new[] { "profissao_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_estabelecimentos_cnpj",
                schema: "public",
                table: "estabelecimentos",
                column: "cnpj",
                unique: true,
                filter: "cnpj IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_estabelecimentos_dono",
                schema: "public",
                table: "estabelecimentos",
                column: "dono_usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exame_fisico_estabelecimento",
                schema: "public",
                table: "exame_fisico",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_exame_fisico_evolucao",
                schema: "public",
                table: "exame_fisico",
                column: "evolucao_id");

            migrationBuilder.CreateIndex(
                name: "ix_exame_fisico_paciente_realizado",
                schema: "public",
                table: "exame_fisico",
                columns: new[] { "paciente_id", "realizado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_exame_fisico_prontuario_realizado",
                schema: "public",
                table: "exame_fisico",
                columns: new[] { "prontuario_id", "realizado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_exame_fisico_regiao_codigo",
                schema: "public",
                table: "exame_fisico_regioes",
                columns: new[] { "exame_fisico_id", "regiao_codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_forma_pagamento_estab_ativo",
                schema: "public",
                table: "formas_pagamento",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "uq_forma_pagamento_estab_nome",
                schema: "public",
                table: "formas_pagamento",
                columns: new[] { "estabelecimento_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventario_estab_ativo",
                schema: "public",
                table: "itens_inventario",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "uq_inventario_codigo_por_estab",
                schema: "public",
                table: "itens_inventario",
                columns: new[] { "estabelecimento_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_orcamento_orcamento",
                schema: "public",
                table: "itens_orcamento",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_agendados_status_proximo_run",
                schema: "public",
                table: "jobs_agendados",
                columns: new[] { "status", "proximo_run_em" });

            migrationBuilder.CreateIndex(
                name: "uq_jobs_agendados_nome",
                schema: "public",
                table: "jobs_agendados",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lancamento_estab_status_venc",
                schema: "public",
                table: "lancamentos",
                columns: new[] { "estabelecimento_id", "status", "data_vencimento" });

            migrationBuilder.CreateIndex(
                name: "ix_lancamento_estab_tipo",
                schema: "public",
                table: "lancamentos",
                columns: new[] { "estabelecimento_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_orcamento_id",
                schema: "public",
                table: "lancamentos",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_lgpd_anonimizacoes_motivo_data",
                schema: "public",
                table: "lgpd_anonimizacoes",
                columns: new[] { "motivo", "anonimizado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_lgpd_anonimizacoes_tabela_registro",
                schema: "public",
                table: "lgpd_anonimizacoes",
                columns: new[] { "tabela", "registro_id" });

            migrationBuilder.CreateIndex(
                name: "ix_lgpd_consentimentos_usuario_tipo_data",
                schema: "public",
                table: "lgpd_consentimentos",
                columns: new[] { "usuario_id", "tipo", "aceito_em" });

            migrationBuilder.CreateIndex(
                name: "IX_lista_espera_agendamento_atendido_por_agendamento_id",
                schema: "public",
                table: "lista_espera_agendamento",
                column: "atendido_por_agendamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_lista_espera_agendamento_paciente_id",
                schema: "public",
                table: "lista_espera_agendamento",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_lista_espera_estab_atendido",
                schema: "public",
                table: "lista_espera_agendamento",
                columns: new[] { "estabelecimento_id", "atendido_em" });

            migrationBuilder.CreateIndex(
                name: "ix_medicamentos_favoritos_ranking",
                schema: "public",
                table: "medicamentos_favoritos",
                columns: new[] { "profissional_usuario_id", "estabelecimento_id", "uso_count" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "uq_medicamentos_favoritos_chave",
                schema: "public",
                table: "medicamentos_favoritos",
                columns: new[] { "profissional_usuario_id", "estabelecimento_id", "medicamento", "posologia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_modelo_prontuario_estabelecimento",
                schema: "public",
                table: "modelo_de_prontuario",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_modelo_prontuario_padrao_sistema",
                schema: "public",
                table: "modelo_de_prontuario",
                column: "eh_padrao_sistema");

            migrationBuilder.CreateIndex(
                name: "ix_modelo_permissao_estabelecimento",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "uq_modelo_permissao_nome_por_estabelecimento",
                schema: "public",
                table: "modelo_permissao_estabelecimento",
                columns: new[] { "estabelecimento_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_estab_data",
                schema: "public",
                table: "movimentacoes_estoque",
                columns: new[] { "estabelecimento_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_item_data",
                schema: "public",
                table: "movimentacoes_estoque",
                columns: new[] { "item_inventario_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_notificacoes_estabelecimento_criada",
                schema: "public",
                table: "notificacoes",
                columns: new[] { "estabelecimento_id", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_notificacoes_usuario_lida_criada",
                schema: "public",
                table: "notificacoes",
                columns: new[] { "usuario_id", "lida", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_cirurgia_estab_ativo",
                schema: "public",
                table: "orcamento_catalogo_cirurgia",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_catalogo_cirurgia_produto_catalogo_produto_id",
                schema: "public",
                table: "orcamento_catalogo_cirurgia_produto",
                column: "catalogo_produto_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalogo_cirurgia_produto",
                schema: "public",
                table: "orcamento_catalogo_cirurgia_produto",
                columns: new[] { "catalogo_cirurgia_id", "catalogo_produto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_equipe_estab_ativo",
                schema: "public",
                table: "orcamento_catalogo_equipe",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_implante_estab_ativo",
                schema: "public",
                table: "orcamento_catalogo_implante",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_catalogo_implante_item_inventario_id",
                schema: "public",
                table: "orcamento_catalogo_implante",
                column: "item_inventario_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalogo_produto_estab_ativo",
                schema: "public",
                table: "orcamento_catalogo_produto",
                columns: new[] { "estabelecimento_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_cirurgia_orcamento",
                schema: "public",
                table: "orcamento_cirurgias",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_cirurgias_procedimento_cirurgico_id",
                schema: "public",
                table: "orcamento_cirurgias",
                column: "procedimento_cirurgico_id");

            migrationBuilder.CreateIndex(
                name: "uq_config_local_estab_tipo",
                schema: "public",
                table: "orcamento_configuracao_local_cirurgia",
                columns: new[] { "estabelecimento_id", "tipo_internacao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_configuracao_pagamento_forma_pagamento_id",
                schema: "public",
                table: "orcamento_configuracao_pagamento",
                column: "forma_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "uq_config_pgto_estab_forma",
                schema: "public",
                table: "orcamento_configuracao_pagamento",
                columns: new[] { "estabelecimento_id", "forma_pagamento_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_orcamento_equipe_orcamento_profissional_papel",
                schema: "public",
                table: "orcamento_equipe",
                columns: new[] { "orcamento_id", "profissional_usuario_id", "papel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_forma_pagamento_orcamento",
                schema: "public",
                table: "orcamento_formas_pagamento",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_formas_pagamento_forma_pagamento_id",
                schema: "public",
                table: "orcamento_formas_pagamento",
                column: "forma_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_implante_orcamento",
                schema: "public",
                table: "orcamento_implantes",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_implantes_item_inventario_id",
                schema: "public",
                table: "orcamento_implantes",
                column: "item_inventario_id");

            migrationBuilder.CreateIndex(
                name: "ix_valor_prof_orc_estab_prof_funcao",
                schema: "public",
                table: "orcamento_valor_profissional",
                columns: new[] { "estabelecimento_id", "profissional_usuario_id", "funcao" });

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_estab_paciente",
                schema: "public",
                table: "orcamentos",
                columns: new[] { "estabelecimento_id", "paciente_id" });

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_estab_status",
                schema: "public",
                table: "orcamentos",
                columns: new[] { "estabelecimento_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_orcamento_procedimento_cirurgico",
                schema: "public",
                table: "orcamentos",
                column: "procedimento_cirurgico_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamentos_paciente_id",
                schema: "public",
                table: "orcamentos",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_paciente_acesso_log_estab_data",
                schema: "public",
                table: "paciente_acesso_log",
                columns: new[] { "estabelecimento_id", "ocorrido_em" });

            migrationBuilder.CreateIndex(
                name: "ix_paciente_acesso_log_paciente_data",
                schema: "public",
                table: "paciente_acesso_log",
                columns: new[] { "paciente_id", "ocorrido_em" });

            migrationBuilder.CreateIndex(
                name: "ix_paciente_acesso_log_usuario",
                schema: "public",
                table: "paciente_acesso_log",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_estabelecimento",
                schema: "public",
                table: "pacientes",
                columns: new[] { "estabelecimento_id", "deletado_em" });

            migrationBuilder.CreateIndex(
                name: "uq_pacientes_estabelecimento_cpf",
                schema: "public",
                table: "pacientes",
                columns: new[] { "estabelecimento_id", "cpf" },
                unique: true,
                filter: "cpf IS NOT NULL AND deletado_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_pacientes_estabelecimento_doc_internacional",
                schema: "public",
                table: "pacientes",
                columns: new[] { "estabelecimento_id", "documento_internacional" },
                unique: true,
                filter: "documento_internacional IS NOT NULL AND deletado_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_planos_nome",
                schema: "public",
                table: "planos",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_procedimento_estab_data_agendada",
                schema: "public",
                table: "procedimentos_cirurgicos",
                columns: new[] { "estabelecimento_id", "data_agendada" });

            migrationBuilder.CreateIndex(
                name: "ix_procedimento_paciente_data_realizada",
                schema: "public",
                table: "procedimentos_cirurgicos",
                columns: new[] { "paciente_id", "data_realizada" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_procedimentos_cirurgicos_agendamento_id",
                schema: "public",
                table: "procedimentos_cirurgicos",
                column: "agendamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_procedimentos_cirurgicos_prontuario_id",
                schema: "public",
                table: "procedimentos_cirurgicos",
                column: "prontuario_id");

            migrationBuilder.CreateIndex(
                name: "uq_profissionais_conselho_uf_numero",
                schema: "public",
                table: "profissionais",
                columns: new[] { "conselho", "uf", "numero_registro" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_profissoes_ativo",
                schema: "public",
                table: "profissoes",
                column: "ativo");

            migrationBuilder.CreateIndex(
                name: "uq_profissoes_nome",
                schema: "public",
                table: "profissoes",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_acesso_log_prontuario_data",
                schema: "public",
                table: "prontuario_acesso_log",
                columns: new[] { "prontuario_id", "ocorrido_em" });

            migrationBuilder.CreateIndex(
                name: "ix_acesso_log_usuario",
                schema: "public",
                table: "prontuario_acesso_log",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_anexos_evolucao",
                schema: "public",
                table: "prontuario_anexos",
                column: "evolucao_id");

            migrationBuilder.CreateIndex(
                name: "ix_anexos_prontuario",
                schema: "public",
                table: "prontuario_anexos",
                columns: new[] { "prontuario_id", "arquivado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_evolucoes_prontuario_data",
                schema: "public",
                table: "prontuario_evolucoes",
                columns: new[] { "prontuario_id", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_pool_estabelecimento_tipo",
                schema: "public",
                table: "prontuario_variaveis_pool",
                columns: new[] { "estabelecimento_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "ix_pool_padrao_tipo",
                schema: "public",
                table: "prontuario_variaveis_pool",
                columns: new[] { "eh_padrao_sistema", "tipo" });

            migrationBuilder.CreateIndex(
                name: "uq_prontuario_paciente_estabelecimento",
                schema: "public",
                table: "prontuarios",
                columns: new[] { "paciente_id", "estabelecimento_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receita_itens_receita_ordem",
                schema: "public",
                table: "receita_itens",
                columns: new[] { "receita_id", "ordem" });

            migrationBuilder.CreateIndex(
                name: "ix_receitas_estab_prof_emitida",
                schema: "public",
                table: "receitas",
                columns: new[] { "estabelecimento_id", "profissional_usuario_id", "emitida_em" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_receitas_paciente_emitida",
                schema: "public",
                table: "receitas",
                columns: new[] { "paciente_id", "emitida_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_regioes_anatomicas_catalogo_ativo_vista",
                schema: "public",
                table: "regioes_anatomicas_catalogo",
                columns: new[] { "ativo", "vista" });

            migrationBuilder.CreateIndex(
                name: "ix_regioes_anatomicas_catalogo_vista",
                schema: "public",
                table: "regioes_anatomicas_catalogo",
                column: "vista");

            migrationBuilder.CreateIndex(
                name: "uq_regioes_anatomicas_catalogo_codigo",
                schema: "public",
                table: "regioes_anatomicas_catalogo",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sala_atendimento_tipo_sala_id",
                schema: "public",
                table: "sala_atendimento",
                column: "tipo_sala_id");

            migrationBuilder.CreateIndex(
                name: "ix_salas_estab",
                schema: "public",
                table: "sala_atendimento",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_salas_unidade",
                schema: "public",
                table: "sala_atendimento",
                column: "unidade_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_vinculo_estab_status_data",
                schema: "public",
                table: "solicitacoes_vinculo",
                columns: new[] { "estabelecimento_id", "status", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_vinculo_profissional_status",
                schema: "public",
                table: "solicitacoes_vinculo",
                columns: new[] { "profissional_usuario_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_solicitacoes_vinculo_pendente",
                schema: "public",
                table: "solicitacoes_vinculo",
                columns: new[] { "profissional_usuario_id", "estabelecimento_id" },
                unique: true,
                filter: "status = 'Pendente'");

            migrationBuilder.CreateIndex(
                name: "uq_tipo_sala_nome",
                schema: "public",
                table: "tipo_sala_atendimento",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unidades_estab",
                schema: "public",
                table: "unidades_estabelecimento",
                column: "estabelecimento_id");

            migrationBuilder.CreateIndex(
                name: "uq_unidades_principal_por_estab",
                schema: "public",
                table: "unidades_estabelecimento",
                column: "estabelecimento_id",
                unique: true,
                filter: "is_principal = true");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                schema: "public",
                table: "usuarios",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "uq_usuarios_cpf",
                schema: "public",
                table: "usuarios",
                column: "cpf",
                unique: true,
                filter: "cpf IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vinculo_estabelecimento_status",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                columns: new[] { "estabelecimento_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_vinculo_profissional_status",
                schema: "public",
                table: "vinculo_profissional_estabelecimento",
                columns: new[] { "profissional_usuario_id", "status" });


            // ============================================================
            // SQL custom (sem par EF — tabelas auxiliares acessadas via Dapper)
            // ============================================================

            // ---- AI cache + AI rate limits (acessadas via Dapper) ----
            migrationBuilder.Sql(@"-- Item 1.7 (Docs/01_FASE_1_HARDENING.md) — tabelas auxiliares de IA sem aggregate EF.
--
-- Estas tabelas são acessadas via Dapper (raw SQL) por AiCacheRepository
-- e AiRateLimitRepository. NÃO há entidade EF correspondente, portanto
-- nunca aparecem em uma migration EF — vivem como SQL puro em
-- db/migrations/.
--
-- Idempotência: usamos CREATE TABLE IF NOT EXISTS / CREATE INDEX IF NOT
-- EXISTS para casar com a estratégia idempotente das migrations
-- (mesmo padrão do EF, sem precisar do registro em __ef_migrations_history).
--
-- Pendente: RLS policies. Como ai_outputs_cache e ai_rate_limits são
-- tabelas internas usadas só pelo backend (service_role), o caminho
-- esperado é ""REVOKE ALL FROM authenticated/anon"" — fica para a sprint
-- de RLS na Fase 2 junto com ai_audit_logs e audit_delete_attempts.

-- ---------------------------------------------------------------------
-- ai_outputs_cache
-- ---------------------------------------------------------------------
-- Cache de respostas da IA chaveado por hash sha256 do prompt.
-- prompt_hash é PK porque o mesmo prompt produz a mesma resposta
-- determinística (dentro da janela de TTL).
CREATE TABLE IF NOT EXISTS public.ai_outputs_cache (
    prompt_hash         character varying(64) NOT NULL,
    estabelecimento_id  bigint                NOT NULL,
    endpoint            character varying(80) NOT NULL,
    output              text                  NOT NULL,
    tokens_in           integer,
    tokens_out          integer,
    expira_em           timestamp with time zone NOT NULL,
    criado_em           timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_ai_outputs_cache PRIMARY KEY (prompt_hash)
);

-- Índice para o job de limpeza (DELETE WHERE expira_em < now()).
CREATE INDEX IF NOT EXISTS ix_ai_outputs_cache_expira
    ON public.ai_outputs_cache (expira_em);

-- Índice multi-tenant (consultas filtradas por estabelecimento + endpoint
-- caso virem necessárias para auditoria/relatório).
CREATE INDEX IF NOT EXISTS ix_ai_outputs_cache_estab_endpoint
    ON public.ai_outputs_cache (estabelecimento_id, endpoint);

-- ---------------------------------------------------------------------
-- ai_rate_limits
-- ---------------------------------------------------------------------
-- Janela deslizante de 1 minuto por usuário.
-- (usuario_id, periodo_inicio) é único — uma linha por usuário por
-- janela. O backend faz UPSERT incrementando ""contagem"" e atualizando
-- ""ultimo_acesso"".
CREATE TABLE IF NOT EXISTS public.ai_rate_limits (
    id              bigint GENERATED BY DEFAULT AS IDENTITY,
    usuario_id      uuid                     NOT NULL,
    periodo_inicio  timestamp with time zone NOT NULL,
    contagem        integer                  NOT NULL DEFAULT 1,
    ultimo_acesso   timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_ai_rate_limits PRIMARY KEY (id),
    CONSTRAINT uq_ai_rate_limits_usuario_periodo UNIQUE (usuario_id, periodo_inicio)
);

-- Índice para varredura de limpeza de janelas antigas
-- (DELETE WHERE periodo_inicio < now() - interval '1 hour').
CREATE INDEX IF NOT EXISTS ix_ai_rate_limits_periodo
    ON public.ai_rate_limits (periodo_inicio);");

            // ---- EXCLUDE constraint: defense-in-depth contra overlap de agendamento ----
            migrationBuilder.Sql(@"-- Item 2.15 — Fase 2: Defense-in-depth contra overlap de agendamento.
-- Bloqueia, no nivel do banco, dois agendamentos do mesmo profissional
-- com intervalos [inicio_previsto, fim_previsto) sobrepostos.
-- Complementa o check do handler (item 1.2 da Fase 1) — o banco eh a fonte
-- final de verdade caso o handler tenha bug, race condition ou bypass.
--
-- Linhas com status='Cancelado' sao excluidas do constraint (WHERE clause)
-- pois um horario cancelado nao deve impedir reagendamento no mesmo slot.
-- Status atuais (verificado em prod): Agendado, Concluido, Cancelado.

CREATE EXTENSION IF NOT EXISTS btree_gist;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'agendamentos_no_overlap'
          AND conrelid = 'public.agendamentos'::regclass
    ) THEN
        ALTER TABLE public.agendamentos
            ADD CONSTRAINT agendamentos_no_overlap
            EXCLUDE USING gist (
                profissional_usuario_id WITH =,
                tstzrange(inicio_previsto, fim_previsto, '[)') WITH &&
            )
            WHERE (status <> 'Cancelado');
    END IF;
END
$$;");

            // ---- LGPD audit view ----
            migrationBuilder.Sql(@"-- LGPD — VIEW lgpd_acesso_log apontando para prontuario_acesso_log.
--
-- Justificativa: Art. 46 LGPD exige audit trail de acesso a dados pessoais.
-- A tabela prontuario_acesso_log já registra cada leitura/escrita sensível
-- (impl. via IProntuarioAcessoLogService desde a Fase 1). Esta VIEW expõe os
-- mesmos dados sob o nome canonico ""lgpd_acesso_log"" para alinhar com a
-- nomenclatura do legado e facilitar consultas LGPD/compliance.
--
-- Sem par EF — SQL puro. service_role acessa para auditoria; RLS bloqueia
-- authenticated/anon (audit interno).

CREATE OR REPLACE VIEW public.lgpd_acesso_log AS
SELECT
    id,
    usuario_id,
    estabelecimento_id,
    'prontuarios'::text     AS tabela,
    prontuario_id           AS registro_id,
    tipo_acesso,
    ocorrido_em             AS acessado_em
FROM public.prontuario_acesso_log;

COMMENT ON VIEW public.lgpd_acesso_log IS
    'Audit trail de acesso a dados pessoais (Art. 46 LGPD). View sobre prontuario_acesso_log com nomenclatura canônica.';

-- Permissions: a VIEW herda RLS da tabela base. Negar acesso direto para
-- authenticated/anon — apenas service_role (backend) consulta para auditoria.
-- REVOKE removido (sem auth schema no RDS — backend é a única defesa)");

            // ---- Função imutable_unaccent + índice trigram em pacientes.nome_completo ----
            migrationBuilder.Sql(@"-- ----------------------------------------------------------------------------
-- Indice trigram em pacientes.nome_completo para acelerar busca ILIKE '%X%'.
--
-- Contexto: PacienteQueryRepository.Listar usa
--   nome_completo ILIKE '%' || @Busca || '%'
-- Com B-tree padrao isso vira full-scan filtrado por estabelecimento_id —
-- aceitavel em tenants pequenos, ruim em base de >50k pacientes.
--
-- Solucao: GIN com pg_trgm sobre uma expressao IMMUTABLE de
-- lower(unaccent(nome_completo)). Da match com acento/maiuscula/etc.
--
-- Tres passos:
--   1. Habilitar extensoes pg_trgm + unaccent.
--   2. Criar wrapper IMMUTABLE para unaccent (a funcao nativa e STABLE
--      por causa do dicionario, e GIN exige IMMUTABLE).
--   3. Criar indice GIN.
-- ----------------------------------------------------------------------------

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;

-- Wrapper IMMUTABLE: a funcao unaccent() padrao depende do dicionario
-- (carregado em runtime), entao Postgres a marca como STABLE. Para usar
-- em indice expression, criamos um wrapper que aponta explicitamente para
-- o dicionario 'unaccent' e marcamos IMMUTABLE — seguro porque o dicionario
-- nao muda em produc�o.
CREATE OR REPLACE FUNCTION public.imutable_unaccent(text)
    RETURNS text
    LANGUAGE sql
    IMMUTABLE
    PARALLEL SAFE
    STRICT
AS $$
    SELECT public.unaccent('public.unaccent', $1);
$$;

-- Indice GIN com gin_trgm_ops sobre lower(unaccent(...)).
-- Se ja existir (re-execucao), recria para garantir que aponta para a
-- expressao correta. CONCURRENTLY nao pode rodar dentro de transacao —
-- a pipeline envolve toda migration em transacao por default, entao
-- usamos CREATE simples (lock breve em tabela; aceitavel em janela).
CREATE INDEX IF NOT EXISTS ix_pacientes_nome_completo_trgm
    ON public.pacientes
    USING gin (public.imutable_unaccent(lower(nome_completo)) gin_trgm_ops)
    WHERE deletado_em IS NULL;

-- ----------------------------------------------------------------------------
-- Como o front busca:
--   front -> /api/paciente?busca=Maria
--   PacienteQueryRepository.Listar gera ILIKE '%' || @Busca || '%'.
--
-- Para o planner usar este indice, a query precisa ser reescrita para
-- comparar a EXPRESSAO indexada (lower+unaccent dos dois lados):
--   public.imutable_unaccent(lower(nome_completo)) ILIKE '%' || public.imutable_unaccent(lower(@Busca)) || '%'
--
-- Esse ajuste no SQL Dapper esta no commit que acompanha esta migration.
-- ----------------------------------------------------------------------------");

            // ---- Seed catálogo profissões + especialidades ----
            migrationBuilder.Sql(@"-- Seed catálogo de Profissões e Especialidades brasileiras.
-- Gerado a partir de Application/Catalogo/SeedsCatalogo.cs (Wave 1 / Item 3.6).
-- Idempotente: ON CONFLICT DO NOTHING — seguro para re-executar.
-- NÃO contém BEGIN/COMMIT (gerenciado pela pipeline de migrations).

-- ============================================================
-- 1. Profissões
-- ============================================================
INSERT INTO public.profissoes (nome, conselho_sigla, ativo) VALUES
    ('Médico',                        'CRM',    true),
    ('Dentista',                      'CRO',    true),
    ('Fisioterapeuta',                'CREFITO', true),
    ('Psicólogo',                     'CRP',    true),
    ('Nutricionista',                 'CRN',    true),
    ('Fonoaudiólogo',                 'CRFa',   true),
    ('Enfermeiro',                    'COREN',  true),
    ('Terapeuta Ocupacional',         'CREFITO', true),
    ('Farmacêutico',                  'CRF',    true),
    ('Biomédico',                     'CFBM',   true),
    ('Veterinário',                   'CFMV',   true),
    ('Técnico de Enfermagem',         '',       true),
    ('Educador Físico',               'CONFEF', true),
    ('Assistente Social',             'CRESS',  true),
    ('Médico Veterinário Residente',  '',       true),
    ('Acupunturista',                 '',       true),
    ('Quiropraxista',                 '',       true),
    ('Médico Radiologista',           'CRM',    true),
    ('Cirurgião-Dentista Especialista', 'CRO',  true),
    ('Podólogo',                      '',       true),
    ('Optometrista',                  '',       true),
    ('Psicopedagogo',                 '',       true),
    ('Gerontólogo',                   '',       true),
    ('Homeopata',                     '',       true),
    ('Osteopata',                     '',       true),
    ('Massoterapeuta',                '',       true),
    ('Pilates',                       '',       true),
    ('Neuropsicopedagogo',            '',       true),
    ('Médico do Esporte',             'CRM',    true),
    ('Médico Nutrólogo',              'CRM',    true),
    ('Terapeuta ABA',                 '',       true)
ON CONFLICT (nome) DO NOTHING;

-- ============================================================
-- 2. Especialidades (JOIN pelo nome da profissão para obter ID)
-- ============================================================
WITH prof AS (SELECT id, nome FROM public.profissoes)
INSERT INTO public.especialidades (profissao_id, nome, ativo)
SELECT prof.id, e.nome, true
FROM prof
CROSS JOIN (VALUES
    -- Médico (35)
    ('Médico', 'Clínico Geral'),
    ('Médico', 'Cardiologia'),
    ('Médico', 'Pediatria'),
    ('Médico', 'Ginecologia e Obstetrícia'),
    ('Médico', 'Ortopedia e Traumatologia'),
    ('Médico', 'Dermatologia'),
    ('Médico', 'Endocrinologia'),
    ('Médico', 'Psiquiatria'),
    ('Médico', 'Gastroenterologia'),
    ('Médico', 'Oftalmologia'),
    ('Médico', 'Geriatria'),
    ('Médico', 'Neurologia'),
    ('Médico', 'Anestesiologia'),
    ('Médico', 'Cirurgia Geral'),
    ('Médico', 'Cirurgia Plástica'),
    ('Médico', 'Infectologia'),
    ('Médico', 'Nefrologia'),
    ('Médico', 'Pneumologia'),
    ('Médico', 'Reumatologia'),
    ('Médico', 'Urologia'),
    ('Médico', 'Hematologia'),
    ('Médico', 'Oncologia'),
    ('Médico', 'Radiologia'),
    ('Médico', 'Medicina do Trabalho'),
    ('Médico', 'Medicina Esportiva'),
    ('Médico', 'Medicina de Família e Comunidade'),
    ('Médico', 'Alergia e Imunologia'),
    ('Médico', 'Medicina Intensiva'),
    ('Médico', 'Otorrinolaringologia'),
    ('Médico', 'Nutrologia'),
    ('Médico', 'Neonatologia'),
    ('Médico', 'Neurocirurgia'),
    ('Médico', 'Medicina Paliativa'),
    ('Médico', 'Patologia'),
    ('Médico', 'Mastologia'),
    -- Dentista (18)
    ('Dentista', 'Odontologia Geral'),
    ('Dentista', 'Odontopediatria'),
    ('Dentista', 'Ortodontia'),
    ('Dentista', 'Periodontia'),
    ('Dentista', 'Endodontia'),
    ('Dentista', 'Implantodontia'),
    ('Dentista', 'Prótese Dentária'),
    ('Dentista', 'Cirurgia e Traumatologia Bucomaxilofacial'),
    ('Dentista', 'Dentística'),
    ('Dentista', 'Odontologia Estética'),
    ('Dentista', 'Odontogeriatria'),
    ('Dentista', 'Patologia Oral'),
    ('Dentista', 'Radiologia Odontológica'),
    ('Dentista', 'Estomatologia'),
    ('Dentista', 'Harmonização Orofacial'),
    ('Dentista', 'Odontologia do Trabalho'),
    ('Dentista', 'Odontologia Legal'),
    ('Dentista', 'Saúde Coletiva (Odontologia)'),
    -- Fisioterapeuta (13)
    ('Fisioterapeuta', 'Fisioterapia Geral'),
    ('Fisioterapeuta', 'Fisioterapia Traumato-Ortopédica'),
    ('Fisioterapeuta', 'Fisioterapia Neurofuncional'),
    ('Fisioterapeuta', 'Fisioterapia Cardiorrespiratória'),
    ('Fisioterapeuta', 'Fisioterapia Dermatofuncional'),
    ('Fisioterapeuta', 'Fisioterapia Uroginecológica'),
    ('Fisioterapeuta', 'Fisioterapia em Terapia Intensiva'),
    ('Fisioterapeuta', 'Fisioterapia Esportiva'),
    ('Fisioterapeuta', 'Fisioterapia do Trabalho'),
    ('Fisioterapeuta', 'Fisioterapia Aquática'),
    ('Fisioterapeuta', 'Fisioterapia Oncológica'),
    ('Fisioterapeuta', 'Fisioterapia em Gerontologia'),
    ('Fisioterapeuta', 'Fisioterapia Pediátrica'),
    -- Psicólogo (12)
    ('Psicólogo', 'Psicologia Clínica'),
    ('Psicólogo', 'Psicologia Organizacional e do Trabalho'),
    ('Psicólogo', 'Psicologia Escolar e Educacional'),
    ('Psicólogo', 'Psicologia Jurídica'),
    ('Psicólogo', 'Psicologia Social'),
    ('Psicólogo', 'Psicologia Hospitalar'),
    ('Psicólogo', 'Neuropsicologia'),
    ('Psicólogo', 'Psicologia do Esporte'),
    ('Psicólogo', 'Terapia Cognitivo-Comportamental'),
    ('Psicólogo', 'Psicanálise'),
    ('Psicólogo', 'Terapia Familiar'),
    ('Psicólogo', 'Psicologia Infantil'),
    -- Nutricionista (9)
    ('Nutricionista', 'Nutrição Clínica'),
    ('Nutricionista', 'Nutrição Esportiva'),
    ('Nutricionista', 'Nutrição Materno-Infantil'),
    ('Nutricionista', 'Nutrição Oncológica'),
    ('Nutricionista', 'Nutrição em Cardiologia'),
    ('Nutricionista', 'Nutrição em Nefrologia'),
    ('Nutricionista', 'Nutrição Comportamental'),
    ('Nutricionista', 'Nutrição Funcional'),
    ('Nutricionista', 'Fitoterapia Aplicada à Nutrição'),
    -- Fonoaudiólogo (9)
    ('Fonoaudiólogo', 'Fonoaudiologia Geral'),
    ('Fonoaudiólogo', 'Audiologia'),
    ('Fonoaudiólogo', 'Linguagem'),
    ('Fonoaudiólogo', 'Motricidade Orofacial'),
    ('Fonoaudiólogo', 'Voz'),
    ('Fonoaudiólogo', 'Disfagia'),
    ('Fonoaudiólogo', 'Fonoaudiologia Educacional'),
    ('Fonoaudiólogo', 'Fonoaudiologia Neurofuncional'),
    ('Fonoaudiólogo', 'Fonoaudiologia Hospitalar'),
    -- Enfermeiro (13)
    ('Enfermeiro', 'Enfermagem Geral'),
    ('Enfermeiro', 'Enfermagem Obstétrica'),
    ('Enfermeiro', 'Enfermagem Pediátrica'),
    ('Enfermeiro', 'Enfermagem em Centro Cirúrgico'),
    ('Enfermeiro', 'Enfermagem em Terapia Intensiva'),
    ('Enfermeiro', 'Enfermagem do Trabalho'),
    ('Enfermeiro', 'Enfermagem em Cardiologia'),
    ('Enfermeiro', 'Enfermagem em Oncologia'),
    ('Enfermeiro', 'Enfermagem em Nefrologia'),
    ('Enfermeiro', 'Enfermagem em Saúde Mental'),
    ('Enfermeiro', 'Enfermagem em Emergência'),
    ('Enfermeiro', 'Enfermagem Domiciliar'),
    ('Enfermeiro', 'Enfermagem Estética'),
    -- Terapeuta Ocupacional (7)
    ('Terapeuta Ocupacional', 'Terapia Ocupacional Geral'),
    ('Terapeuta Ocupacional', 'Terapia Ocupacional Pediátrica'),
    ('Terapeuta Ocupacional', 'Terapia Ocupacional em Saúde Mental'),
    ('Terapeuta Ocupacional', 'Terapia Ocupacional Neurológica'),
    ('Terapeuta Ocupacional', 'Terapia Ocupacional em Gerontologia'),
    ('Terapeuta Ocupacional', 'Terapia Ocupacional em Reabilitação Física'),
    ('Terapeuta Ocupacional', 'Terapia Ocupacional Social'),
    -- Farmacêutico (7)
    ('Farmacêutico', 'Farmácia Clínica'),
    ('Farmacêutico', 'Farmácia Hospitalar'),
    ('Farmacêutico', 'Farmácia Industrial'),
    ('Farmacêutico', 'Farmácia de Manipulação'),
    ('Farmacêutico', 'Farmacologia Clínica'),
    ('Farmacêutico', 'Toxicologia'),
    ('Farmacêutico', 'Análises Clínicas'),
    -- Biomédico (7)
    ('Biomédico', 'Biomedicina Clínica'),
    ('Biomédico', 'Hematologia'),
    ('Biomédico', 'Microbiologia'),
    ('Biomédico', 'Parasitologia'),
    ('Biomédico', 'Biologia Molecular'),
    ('Biomédico', 'Citologia'),
    ('Biomédico', 'Imunologia'),
    -- Veterinário (8)
    ('Veterinário', 'Clínica de Pequenos Animais'),
    ('Veterinário', 'Clínica de Grandes Animais'),
    ('Veterinário', 'Cirurgia Veterinária'),
    ('Veterinário', 'Dermatologia Veterinária'),
    ('Veterinário', 'Oftalmologia Veterinária'),
    ('Veterinário', 'Oncologia Veterinária'),
    ('Veterinário', 'Cardiologia Veterinária'),
    ('Veterinário', 'Medicina Preventiva Veterinária'),
    -- Técnico de Enfermagem (2)
    ('Técnico de Enfermagem', 'Técnico em Enfermagem Geral'),
    ('Técnico de Enfermagem', 'Técnico em Saúde Bucal'),
    -- Educador Físico (6)
    ('Educador Físico', 'Personal Trainer'),
    ('Educador Físico', 'Musculação'),
    ('Educador Físico', 'Treinamento Funcional'),
    ('Educador Físico', 'Pilates'),
    ('Educador Físico', 'Natação'),
    ('Educador Físico', 'Corrida e Atletismo'),
    -- Assistente Social (3)
    ('Assistente Social', 'Assistência Social em Saúde'),
    ('Assistente Social', 'Assistência Social Hospitalar'),
    ('Assistente Social', 'Assistência Social Oncológica'),
    -- Médico Veterinário Residente (2)
    ('Médico Veterinário Residente', 'Residência em Pequenos Animais'),
    ('Médico Veterinário Residente', 'Residência em Grandes Animais'),
    -- Acupunturista (3)
    ('Acupunturista', 'Acupuntura Tradicional Chinesa'),
    ('Acupunturista', 'Auriculoterapia'),
    ('Acupunturista', 'Moxibustão'),
    -- Quiropraxista (3)
    ('Quiropraxista', 'Quiropraxia Geral'),
    ('Quiropraxista', 'Quiropraxia Esportiva'),
    ('Quiropraxista', 'Quiropraxia Pediátrica'),
    -- Médico Radiologista (5)
    ('Médico Radiologista', 'Radiologia Geral'),
    ('Médico Radiologista', 'Tomografia Computadorizada'),
    ('Médico Radiologista', 'Ressonância Magnética'),
    ('Médico Radiologista', 'Ultrassonografia'),
    ('Médico Radiologista', 'Mamografia'),
    -- Cirurgião-Dentista Especialista (2)
    ('Cirurgião-Dentista Especialista', 'Disfunção Temporomandibular'),
    ('Cirurgião-Dentista Especialista', 'Odontologia do Sono'),
    -- Podólogo (3)
    ('Podólogo', 'Podologia Geral'),
    ('Podólogo', 'Podologia Esportiva'),
    ('Podólogo', 'Podologia em Diabetologia'),
    -- Optometrista (3)
    ('Optometrista', 'Optometria Clínica'),
    ('Optometrista', 'Visão Subnormal'),
    ('Optometrista', 'Lentes de Contato'),
    -- Psicopedagogo (2)
    ('Psicopedagogo', 'Psicopedagogia Clínica'),
    ('Psicopedagogo', 'Psicopedagogia Institucional'),
    -- Gerontólogo (2)
    ('Gerontólogo', 'Gerontologia Clínica'),
    ('Gerontólogo', 'Gerontologia Social'),
    -- Homeopata (2)
    ('Homeopata', 'Homeopatia Clínica'),
    ('Homeopata', 'Homeopatia Veterinária'),
    -- Osteopata (3)
    ('Osteopata', 'Osteopatia Geral'),
    ('Osteopata', 'Osteopatia Visceral'),
    ('Osteopata', 'Osteopatia Craniosacral'),
    -- Massoterapeuta (3)
    ('Massoterapeuta', 'Massoterapia Relaxante'),
    ('Massoterapeuta', 'Massoterapia Terapêutica'),
    ('Massoterapeuta', 'Drenagem Linfática'),
    -- Pilates (4)
    ('Pilates', 'Pilates Solo'),
    ('Pilates', 'Pilates com Aparelhos'),
    ('Pilates', 'Pilates para Gestantes'),
    ('Pilates', 'Pilates Clínico'),
    -- Neuropsicopedagogo (2)
    ('Neuropsicopedagogo', 'Neuropsicopedagogia Clínica'),
    ('Neuropsicopedagogo', 'Neuropsicopedagogia Institucional'),
    -- Médico do Esporte (3)
    ('Médico do Esporte', 'Medicina Esportiva Clínica'),
    ('Médico do Esporte', 'Fisiologia do Exercício'),
    ('Médico do Esporte', 'Prevenção de Lesões'),
    -- Médico Nutrólogo (3)
    ('Médico Nutrólogo', 'Nutrologia Clínica'),
    ('Médico Nutrólogo', 'Nutrologia do Esporte'),
    ('Médico Nutrólogo', 'Nutrologia Pediátrica'),
    -- Terapeuta ABA (2)
    ('Terapeuta ABA', 'Análise do Comportamento Aplicada'),
    ('Terapeuta ABA', 'Terapia ABA para TEA')
) AS e(profissao_nome, nome)
WHERE prof.nome = e.profissao_nome
ON CONFLICT (profissao_id, nome) DO NOTHING;");        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_audit_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "assinaturas",
                schema: "public");

            migrationBuilder.DropTable(
                name: "audit_delete_attempts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "auth_email_tokens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "auth_refresh_tokens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "automation_events",
                schema: "public");

            migrationBuilder.DropTable(
                name: "catalogo_procedimentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "categorias_financeiras",
                schema: "public");

            migrationBuilder.DropTable(
                name: "configuracoes_automacao",
                schema: "public");

            migrationBuilder.DropTable(
                name: "equipe_cirurgica",
                schema: "public");

            migrationBuilder.DropTable(
                name: "especialidades",
                schema: "public");

            migrationBuilder.DropTable(
                name: "establishment_ai_settings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "exame_fisico_regioes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "public");

            migrationBuilder.DropTable(
                name: "itens_orcamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "jobs_agendados",
                schema: "public");

            migrationBuilder.DropTable(
                name: "lancamentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "lgpd_anonimizacoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "lgpd_consentimentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "lista_espera_agendamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "medicamentos_favoritos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "modelo_de_prontuario",
                schema: "public");

            migrationBuilder.DropTable(
                name: "modelo_permissao_estabelecimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "movimentacoes_estoque",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notificacoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_anestesia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_catalogo_cirurgia_produto",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_catalogo_equipe",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_catalogo_implante",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_cirurgias",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_configuracao_local_cirurgia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_configuracao_pagamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_equipe",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_formas_pagamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_implantes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_internacao",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_valor_profissional",
                schema: "public");

            migrationBuilder.DropTable(
                name: "paciente_acesso_log",
                schema: "public");

            migrationBuilder.DropTable(
                name: "profissionais",
                schema: "public");

            migrationBuilder.DropTable(
                name: "prontuario_acesso_log",
                schema: "public");

            migrationBuilder.DropTable(
                name: "prontuario_anexos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "prontuario_evolucoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "prontuario_variaveis_pool",
                schema: "public");

            migrationBuilder.DropTable(
                name: "receita_itens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "receitas_configuracao_estabelecimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "regioes_anatomicas_catalogo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sala_atendimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "solicitacoes_vinculo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "public");

            migrationBuilder.DropTable(
                name: "vinculo_profissional_estabelecimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "planos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "auth_credenciais",
                schema: "public");

            migrationBuilder.DropTable(
                name: "automation_rules",
                schema: "public");

            migrationBuilder.DropTable(
                name: "profissoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "exame_fisico",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_catalogo_cirurgia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamento_catalogo_produto",
                schema: "public");

            migrationBuilder.DropTable(
                name: "formas_pagamento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "itens_inventario",
                schema: "public");

            migrationBuilder.DropTable(
                name: "orcamentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "receitas",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tipo_sala_atendimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "unidades_estabelecimento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "procedimentos_cirurgicos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "agendamentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "prontuarios",
                schema: "public");

            migrationBuilder.DropTable(
                name: "estabelecimentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "pacientes",
                schema: "public");
        }
    }
}
