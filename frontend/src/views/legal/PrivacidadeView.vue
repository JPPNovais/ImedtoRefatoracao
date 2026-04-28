<script setup lang="ts">
// Sem lógica — conteúdo estático
</script>

<template>
    <div class="legal">
        <header class="legal-header">
            <router-link :to="{ name: 'Landing' }" class="voltar">← Voltar</router-link>
            <span class="logo">Imedto</span>
        </header>

        <article class="conteudo">
            <h1>Política de Privacidade</h1>
            <p class="data-vigencia">Vigência: 18 de abril de 2026</p>

            <section>
                <h2>1. Quem somos</h2>
                <p>
                    O Imedto é uma plataforma de gestão clínica desenvolvida para profissionais e estabelecimentos
                    de saúde no Brasil. Para fins desta Política, "Imedto", "nós" ou "nosso" refere-se ao operador
                    desta plataforma. O estabelecimento de saúde que utiliza o Imedto atua como Controlador dos dados
                    de seus pacientes; o Imedto atua como Operador, nos termos do art. 5º, inciso VII, da Lei
                    nº 13.709/2018 (LGPD).
                </p>
            </section>

            <section>
                <h2>2. Dados coletados</h2>
                <p>Coletamos as seguintes categorias de dados:</p>
                <ul>
                    <li><strong>Dados de cadastro:</strong> nome completo, endereço de e-mail, telefone e número de registro profissional (CRM, CRO, COREN etc.).</li>
                    <li><strong>Dados de pacientes:</strong> nome, data de nascimento, CPF, telefone, endereço e informações de prontuário eletrônico (anamnese, evolução clínica, procedimentos realizados). Esses dados são classificados como <strong>dados pessoais sensíveis</strong> nos termos do art. 5º, inciso II, da LGPD.</li>
                    <li><strong>Dados financeiros:</strong> registros de receitas, despesas, orçamentos e lançamentos do estabelecimento. Não armazenamos dados de cartão de crédito.</li>
                    <li><strong>Dados de uso:</strong> logs de acesso, endereço IP, tipo de dispositivo e interações com a plataforma, usados exclusivamente para segurança e melhoria do serviço.</li>
                </ul>
            </section>

            <section>
                <h2>3. Base legal para o tratamento</h2>
                <p>O tratamento dos dados é realizado com base nas seguintes hipóteses legais da LGPD:</p>
                <ul>
                    <li><strong>Execução de contrato</strong> (art. 7º, V) — para prestação dos serviços contratados.</li>
                    <li><strong>Cumprimento de obrigação legal</strong> (art. 7º, II) — para atender obrigações regulatórias do setor de saúde, incluindo a Resolução CFM nº 2.299/2021 sobre prontuário eletrônico.</li>
                    <li><strong>Tutela da saúde</strong> (art. 11, II, "f") — para dados sensíveis de pacientes tratados por profissionais habilitados no exercício de atividade de saúde.</li>
                    <li><strong>Legítimo interesse</strong> (art. 7º, IX) — para prevenção de fraudes e segurança da plataforma.</li>
                </ul>
            </section>

            <section>
                <h2>4. Como utilizamos os dados</h2>
                <ul>
                    <li>Fornecer, operar e melhorar os serviços da plataforma.</li>
                    <li>Permitir o gerenciamento de agenda, prontuário, financeiro e estoque.</li>
                    <li>Enviar notificações transacionais relevantes ao uso do sistema.</li>
                    <li>Garantir a segurança da conta e prevenir acessos não autorizados.</li>
                    <li>Cumprir obrigações legais e regulatórias aplicáveis.</li>
                </ul>
                <p>Não utilizamos dados de pacientes para publicidade, venda de dados a terceiros ou qualquer finalidade não relacionada à prestação dos serviços contratados.</p>
            </section>

            <section>
                <h2>5. Compartilhamento de dados</h2>
                <p>
                    Os dados são compartilhados apenas com prestadores de serviço essenciais à operação da plataforma
                    (infraestrutura de nuvem, autenticação) que atuam como suboperadores e estão sujeitos a obrigações
                    contratuais de confidencialidade e segurança equivalentes às desta Política. Não compartilhamos
                    dados com terceiros para fins comerciais.
                </p>
                <p>
                    Poderemos divulgar dados quando exigido por lei, ordem judicial ou autoridade competente.
                </p>
            </section>

            <section>
                <h2>6. Direitos do titular (art. 18 da LGPD)</h2>
                <p>
                    Usuários da plataforma e pacientes cujos dados são tratados têm os seguintes direitos, exercíveis
                    mediante solicitação ao estabelecimento de saúde responsável (Controlador) ou diretamente ao Imedto:
                </p>
                <ul>
                    <li>Confirmação da existência de tratamento;</li>
                    <li>Acesso aos dados;</li>
                    <li>Correção de dados incompletos, inexatos ou desatualizados;</li>
                    <li>Anonimização, bloqueio ou eliminação de dados desnecessários ou tratados em desconformidade;</li>
                    <li>Portabilidade dos dados;</li>
                    <li>Eliminação dos dados tratados com base no consentimento;</li>
                    <li>Informação sobre compartilhamento com terceiros;</li>
                    <li>Revogação do consentimento.</li>
                </ul>
                <p>
                    Profissionais de saúde podem exportar e excluir seus dados diretamente na seção
                    <strong>Minha Conta</strong> da plataforma.
                </p>
            </section>

            <section>
                <h2>7. Segurança da informação</h2>
                <p>
                    Adotamos medidas técnicas e organizacionais adequadas para proteger os dados contra acesso não
                    autorizado, perda, alteração ou destruição, incluindo: criptografia em trânsito (TLS 1.2+),
                    autenticação segura com tokens de vida curta, controle de acesso baseado em papéis (RBAC) e
                    isolamento de dados por estabelecimento.
                </p>
            </section>

            <section>
                <h2>8. Retenção de dados</h2>
                <p>
                    Dados de prontuário são retidos pelo prazo mínimo exigido pelo CFM (20 anos a partir do último
                    atendimento, ou 5 anos após a maioridade do paciente, o que for maior). Demais dados são retidos
                    pelo prazo necessário às finalidades descritas nesta Política ou exigências legais aplicáveis.
                    Após o encerramento do contrato, os dados são anonimizados ou eliminados dentro de 90 dias, salvo
                    obrigação legal de retenção.
                </p>
            </section>

            <section>
                <h2>9. Cookies e tecnologias similares</h2>
                <p>
                    Utilizamos cookies de sessão estritamente necessários para autenticação segura. Não utilizamos
                    cookies de rastreamento ou publicidade.
                </p>
            </section>

            <section>
                <h2>10. Alterações nesta Política</h2>
                <p>
                    Esta Política pode ser atualizada periodicamente. Alterações materiais serão comunicadas por
                    e-mail com antecedência mínima de 30 dias. O uso continuado da plataforma após a vigência da nova
                    versão constitui aceitação das alterações.
                </p>
            </section>

            <section>
                <h2>11. Contato</h2>
                <p>
                    Para exercer seus direitos ou esclarecer dúvidas sobre esta Política, entre em contato com o
                    Encarregado de Proteção de Dados (DPO) pelo e-mail
                    <a href="mailto:privacidade@imedto.com.br">privacidade@imedto.com.br</a>.
                </p>
            </section>
        </article>

        <footer class="legal-footer">
            <router-link :to="{ name: 'Landing' }">← Voltar ao início</router-link>
            <router-link :to="{ name: 'Termos' }">Termos de Uso →</router-link>
        </footer>
    </div>
</template>

<style scoped>
.legal {
    max-width: 780px;
    margin: 0 auto;
    padding: 0 1rem 4rem;
    background: #fff;
    min-height: 100vh;
}

.legal-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem 0;
    border-bottom: 1px solid var(--border);
    margin-bottom: 2.5rem;
}

.voltar {
    font-size: 0.87em;
    color: var(--text-muted);
    text-decoration: none;
}

.voltar:hover {
    color: var(--primary);
}

.logo {
    font-size: 1rem;
    font-weight: 700;
    color: var(--primary);
}

.conteudo h1 {
    font-size: 1.8rem;
    font-weight: 800;
    margin: 0 0 0.4rem;
    letter-spacing: -0.4px;
}

.data-vigencia {
    font-size: 0.85em;
    color: var(--text-faint);
    margin: 0 0 2.5rem;
}

.conteudo section {
    margin-bottom: 2rem;
}

.conteudo h2 {
    font-size: 1rem;
    font-weight: 700;
    margin: 0 0 0.6rem;
    color: var(--text);
}

.conteudo p,
.conteudo li {
    font-size: 0.92em;
    line-height: 1.7;
    color: #374151;
    margin: 0 0 0.5rem;
}

.conteudo ul {
    padding-left: 1.25rem;
    margin: 0.4rem 0;
}

.conteudo a {
    color: var(--primary);
}

.legal-footer {
    display: flex;
    justify-content: space-between;
    padding-top: 2rem;
    border-top: 1px solid var(--border);
    font-size: 0.85em;
}

.legal-footer a {
    color: var(--text-muted);
    text-decoration: none;
}

.legal-footer a:hover {
    color: var(--primary);
}
</style>
