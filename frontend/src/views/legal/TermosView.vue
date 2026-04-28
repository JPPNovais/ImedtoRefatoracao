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
            <h1>Termos de Uso</h1>
            <p class="data-vigencia">Vigência: 18 de abril de 2026</p>

            <section>
                <h2>1. Objeto</h2>
                <p>
                    O Imedto é uma plataforma de gestão clínica oferecida como Software como Serviço (SaaS) a
                    profissionais e estabelecimentos de saúde no Brasil. O uso da plataforma implica a aceitação
                    integral destes Termos de Uso e da <router-link :to="{ name: 'Privacidade' }">Política de Privacidade</router-link>.
                </p>
            </section>

            <section>
                <h2>2. Cadastro e acesso</h2>
                <p>
                    Para utilizar o Imedto é necessário criar uma conta com e-mail e senha válidos. O usuário é
                    responsável por manter a confidencialidade de suas credenciais e por todas as atividades
                    realizadas em sua conta. Em caso de acesso não autorizado, o usuário deve nos notificar
                    imediatamente pelo e-mail <a href="mailto:suporte@imedto.com.br">suporte@imedto.com.br</a>.
                </p>
                <p>
                    O Imedto pode recusar ou cancelar o cadastro de qualquer usuário que viole estes Termos,
                    sem necessidade de aviso prévio.
                </p>
            </section>

            <section>
                <h2>3. Uso permitido</h2>
                <p>O Imedto é disponibilizado exclusivamente para:</p>
                <ul>
                    <li>Gestão administrativa e clínica de estabelecimentos de saúde legalmente constituídos no Brasil.</li>
                    <li>Registro de informações de pacientes por profissionais de saúde habilitados nos respectivos conselhos de classe.</li>
                    <li>Controle financeiro, de agenda e de estoque vinculados à atividade clínica.</li>
                </ul>
                <p>
                    É expressamente proibido utilizar a plataforma para finalidades ilícitas, compartilhar dados de
                    pacientes sem consentimento ou base legal, ou tentar comprometer a segurança ou integridade
                    do sistema.
                </p>
            </section>

            <section>
                <h2>4. Responsabilidades do usuário</h2>
                <ul>
                    <li>Garantir a veracidade e atualidade das informações cadastradas.</li>
                    <li>Cumprir a LGPD (Lei nº 13.709/2018) e demais normas aplicáveis ao tratamento de dados de saúde, incluindo a Resolução CFM nº 2.299/2021.</li>
                    <li>Obter as autorizações necessárias dos pacientes para o registro e tratamento de seus dados.</li>
                    <li>Manter backup próprio dos dados críticos do estabelecimento.</li>
                    <li>Garantir que apenas profissionais autorizados tenham acesso ao sistema.</li>
                </ul>
            </section>

            <section>
                <h2>5. Responsabilidades do Imedto</h2>
                <p>O Imedto compromete-se a:</p>
                <ul>
                    <li>Disponibilizar a plataforma com meta de disponibilidade de 99,5% ao mês (exceto manutenções programadas).</li>
                    <li>Proteger os dados armazenados com medidas técnicas e organizacionais adequadas.</li>
                    <li>Notificar o usuário sobre incidentes de segurança que afetem dados de sua conta, conforme prazos da LGPD.</li>
                    <li>Manter backups regulares da infraestrutura de dados.</li>
                </ul>
            </section>

            <section>
                <h2>6. Propriedade intelectual</h2>
                <p>
                    O Imedto e todos os seus componentes (software, design, marca, documentação) são propriedade
                    exclusiva do Imedto ou de seus licenciadores. O presente Termo não transfere ao usuário nenhum
                    direito de propriedade intelectual sobre a plataforma.
                </p>
                <p>
                    Os dados inseridos pelos usuários (dados de pacientes, financeiro, prontuário) pertencem ao
                    estabelecimento de saúde e ao usuário responsável pelo cadastro.
                </p>
            </section>

            <section>
                <h2>7. Pagamento e planos</h2>
                <p>
                    Os planos e condições de pagamento são detalhados na página de preços da plataforma. A falta
                    de pagamento pode resultar na suspensão temporária do acesso. O cancelamento pode ser feito a
                    qualquer momento, sem multa, com efeito ao final do período pago.
                </p>
            </section>

            <section>
                <h2>8. Limitação de responsabilidade</h2>
                <p>
                    O Imedto não é responsável por decisões clínicas tomadas com base nas informações registradas
                    na plataforma, que é uma ferramenta de apoio administrativo e de registro, e não substitui
                    o julgamento clínico do profissional de saúde.
                </p>
                <p>
                    Em nenhuma hipótese o Imedto será responsável por danos indiretos, incidentais, especiais ou
                    consequenciais decorrentes do uso ou da impossibilidade de uso da plataforma, exceto nos casos
                    em que a lei imponha responsabilidade irrenunciável.
                </p>
            </section>

            <section>
                <h2>9. Rescisão</h2>
                <p>
                    O usuário pode encerrar sua conta a qualquer momento na seção <strong>Minha Conta</strong>.
                    O Imedto pode encerrar ou suspender o acesso em caso de violação destes Termos, mediante
                    notificação por e-mail. Após o encerramento, os dados serão retidos conforme a Política de
                    Privacidade e eliminados nos prazos ali previstos.
                </p>
            </section>

            <section>
                <h2>10. Alterações nos Termos</h2>
                <p>
                    Reservamo-nos o direito de alterar estes Termos a qualquer momento. Alterações materiais serão
                    comunicadas com antecedência mínima de 30 dias. O uso continuado da plataforma após a vigência
                    da nova versão implica aceitação das alterações.
                </p>
            </section>

            <section>
                <h2>11. Lei aplicável e foro</h2>
                <p>
                    Estes Termos são regidos pela legislação brasileira. As partes elegem o foro da Comarca de
                    São Paulo/SP para dirimir quaisquer litígios decorrentes deste instrumento, com renúncia a
                    qualquer outro, por mais privilegiado que seja.
                </p>
            </section>

            <section>
                <h2>12. Contato</h2>
                <p>
                    Dúvidas sobre estes Termos podem ser enviadas para
                    <a href="mailto:juridico@imedto.com.br">juridico@imedto.com.br</a>.
                </p>
            </section>
        </article>

        <footer class="legal-footer">
            <router-link :to="{ name: 'Landing' }">← Voltar ao início</router-link>
            <router-link :to="{ name: 'Privacidade' }">Política de Privacidade →</router-link>
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
