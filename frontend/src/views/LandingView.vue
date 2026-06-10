<script setup lang="ts">
import { onMounted } from "vue"
import { useRouter } from "vue-router"
import logo from "@/assets/imedto-logo.png"
import logoBranco from "@/assets/imedto-logo-branco.png"

const router = useRouter()
function irParaLogin() { router.push({ name: "Login" }) }

const features = [
    { icon: "fa-file-medical",        title: "Prontuários Eletrônicos",  desc: "Sistema completo de prontuários médicos com modelos personalizáveis, evolução de pacientes e anexos de exames." },
    { icon: "fa-calendar-check",      title: "Agenda Inteligente",       desc: "Gestão de agendamentos com controle de horários, profissionais e especialidades. Evite conflitos automaticamente." },
    { icon: "fa-users",               title: "Gestão de Pacientes",      desc: "Cadastro completo de pacientes com histórico médico, dados pessoais e documentação organizada em um só lugar." },
    { icon: "fa-user-doctor",         title: "Equipe Médica",            desc: "Gerencie sua equipe de profissionais com controle de vínculos, especialidades e permissões personalizadas." },
    { icon: "fa-shield-halved",       title: "Segurança LGPD",           desc: "Plataforma 100% adequada à LGPD, garantindo a privacidade e segurança dos dados dos pacientes." },
    { icon: "fa-box",                 title: "Controle de Estoque",      desc: "Gerencie produtos, materiais e medicamentos com controle de entrada e saída, alertas de estoque baixo." },
    { icon: "fa-file-invoice-dollar", title: "Orçamentos",               desc: "Crie e gerencie orçamentos para procedimentos, com controle de status e histórico completo." },
    { icon: "fa-building",            title: "Multi-estabelecimento",    desc: "Gerencie múltiplas clínicas ou consultórios em uma única conta, com dados isolados e seguros." },
    { icon: "fa-lock",                title: "Controle de Permissões",   desc: "Defina o que cada profissional pode acessar com modelos de permissão flexíveis e personalizáveis." },
]

const benefits = [
    { stat: "70%",  label: "menos tempo",    title: "Economia de Tempo",  desc: "Automatize tarefas administrativas e reduza o tempo gasto com papelada em até 70%." },
    { stat: "3x",   label: "mais eficiente", title: "Mais Produtividade", desc: "Atenda mais pacientes com processos otimizados e informações sempre à mão." },
    { stat: "100%", label: "digital",        title: "Zero Papel",         desc: "Elimine documentos físicos e tenha tudo digitalizado, organizado e seguro na nuvem." },
    { stat: "24/7", label: "disponível",     title: "Acesso Remoto",      desc: "Acesse seus dados de qualquer lugar, a qualquer hora, de forma segura e confiável." },
]

const steps = [
    { n: "1", title: "Cadastre-se",   desc: "Crie sua conta gratuita em menos de 2 minutos." },
    { n: "2", title: "Configure",     desc: "Personalize seu estabelecimento e adicione sua equipe." },
    { n: "3", title: "Comece a usar", desc: "Cadastre pacientes e comece a atender de forma digital." },
]

const testimonials = [
    { avatar: "CM", name: "Dr. Carlos Mendes", role: "Clínica Saúde+",           text: "O Imedto revolucionou nossa clínica. Conseguimos atender 40% mais pacientes sem aumentar a equipe." },
    { avatar: "AP", name: "Dra. Ana Paula",     role: "Consultório Dermatologia", text: "A gestão de prontuários ficou muito mais fácil. Tudo organizado e acessível em poucos cliques." },
    { avatar: "RS", name: "Dr. Roberto Silva",  role: "Clínica Ortopédica",       text: "Finalmente uma solução que realmente entende as necessidades de uma clínica médica." },
]

const faqItems = [
    { q: "É seguro armazenar dados de pacientes?",  a: "Sim! O Imedto é 100% adequado à LGPD e utiliza criptografia de ponta a ponta. Seus dados estão protegidos em servidores seguros com backup automático." },
    { q: "Preciso instalar algum software?",         a: "Não! O Imedto é totalmente online (cloud). Basta um navegador e conexão com internet para acessar de qualquer dispositivo." },
    { q: "Posso usar em várias clínicas?",           a: "Sim! O sistema é multi-estabelecimento. Você pode gerenciar várias clínicas com uma única conta, mantendo os dados isolados e seguros." },
    { q: "Como funciona o suporte?",                 a: "Oferecemos suporte via chat, e-mail e telefone. Nossa equipe está pronta para ajudar você a aproveitar ao máximo a plataforma." },
]

const planos = [
    { nome: "Essencial",    preco: 149, limite: 3,  destaque: false, features: ["Agenda de consultas","Cadastro de pacientes","Prontuário médico completo","Modelos de prontuário","Minhas consultas","Gestão de profissionais","Controle de permissões","Configurações do estabelecimento"] },
    { nome: "Profissional", preco: 299, limite: 10, destaque: true,  features: ["Tudo do Essencial","Gestão de estoque","Orçamentos cirúrgicos","Módulo financeiro","Relatórios gerenciais"] },
    { nome: "Premium",      preco: 499, limite: 30, destaque: false, features: ["Tudo do Clínica","Automação inteligente","Assistente clínico com IA"] },
]

onMounted(() => {
    const obs = new IntersectionObserver(
        (entries) => entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add("visible"); obs.unobserve(e.target) } }),
        { threshold: 0.1, rootMargin: "0px 0px -80px 0px" }
    )
    document.querySelectorAll(".reveal").forEach(el => obs.observe(el))
})
</script>

<template>
    <div class="landing">
        <!-- Navbar -->
        <header class="navbar">
            <nav class="container nav-inner">
                <img :src="logo" alt="Imedto" class="nav-logo" />
                <ul class="nav-links">
                    <li><a href="#features">Funcionalidades</a></li>
                    <li><a href="#benefits">Benefícios</a></li>
                    <li><a href="#how-it-works">Como funciona</a></li>
                    <li><a href="#pricing">Planos</a></li>
                    <li><a href="#faq">FAQ</a></li>
                </ul>
                <button class="btn-primary" @click="irParaLogin">Começar grátis</button>
            </nav>
        </header>

        <!-- Hero -->
        <section class="hero">
            <div class="container hero-inner">
                <div class="hero-texto animate-fade-in-up">
                    <h1>Gestão médica completa para sua clínica</h1>
                    <p>Prontuários eletrônicos, agenda inteligente, gestão de pacientes e muito mais. Tudo em uma plataforma moderna, segura e fácil de usar.</p>
                    <div class="hero-btns">
                        <button class="btn-primary btn-lg" @click="irParaLogin">Começar agora</button>
                        <a href="#how-it-works" class="btn-outline btn-lg">Como funciona</a>
                    </div>
                    <p class="hero-nota">✓ Sem cartão de crédito &nbsp;&nbsp; ✓ Configuração em minutos</p>
                </div>
                <div class="hero-card animate-fade-in-right animate-float">
                    <div class="hero-card-inner">
                        <div class="hero-card-file">
                            <div class="hero-card-file-header">
                                <div class="hc-avatar"><i class="fa-solid fa-file-medical"></i></div>
                                <div class="hc-lines"><span></span><span></span></div>
                            </div>
                            <div class="hc-body"><span></span><span></span><span></span></div>
                        </div>
                        <div class="hero-card-badges">
                            <div class="hc-badge success"><i class="fa-solid fa-check-circle"></i><span>Agenda otimizada</span></div>
                            <div class="hc-badge info"><i class="fa-solid fa-shield-halved"></i><span>100% seguro</span></div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Features -->
        <section id="features" class="section bg-white">
            <div class="container">
                <div class="section-header">
                    <h2>Tudo que você precisa em um só lugar</h2>
                    <p>Uma plataforma completa para modernizar a gestão da sua clínica ou consultório</p>
                </div>
                <div class="grid-3">
                    <div v-for="(f, i) in features" :key="f.title" class="card reveal" :style="{ transitionDelay: `${i * 60}ms` }">
                        <div class="card-icon"><i :class="`fa-solid ${f.icon}`"></i></div>
                        <h3>{{ f.title }}</h3>
                        <p>{{ f.desc }}</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Benefits -->
        <section id="benefits" class="section bg-light">
            <div class="container">
                <div class="section-header">
                    <h2>Resultados que você vai sentir</h2>
                    <p>Veja como o Imedto pode transformar sua clínica</p>
                </div>
                <div class="grid-4">
                    <div v-for="(b, i) in benefits" :key="b.title" class="benefit-card reveal" :style="{ transitionDelay: `${i * 80}ms` }">
                        <div class="benefit-stat">{{ b.stat }}</div>
                        <div class="benefit-label">{{ b.label }}</div>
                        <h3>{{ b.title }}</h3>
                        <p>{{ b.desc }}</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- How it works -->
        <section id="how-it-works" class="section bg-white">
            <div class="container">
                <div class="section-header">
                    <h2>Como funciona</h2>
                    <p>Comece a usar em 3 passos simples</p>
                </div>
                <div class="grid-3">
                    <div v-for="(s, i) in steps" :key="s.n" class="step-card reveal" :style="{ transitionDelay: `${i * 100}ms` }">
                        <div class="step-num">{{ s.n }}</div>
                        <h3>{{ s.title }}</h3>
                        <p>{{ s.desc }}</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Testimonials -->
        <section class="section bg-light">
            <div class="container">
                <div class="section-header">
                    <h2>O que nossos clientes dizem</h2>
                    <p>Histórias de sucesso de quem já usa o Imedto</p>
                </div>
                <div class="grid-3">
                    <div v-for="(t, i) in testimonials" :key="t.name" class="testimonial reveal" :style="{ transitionDelay: `${i * 80}ms` }">
                        <div class="testimonial-header">
                            <div class="t-avatar">{{ t.avatar }}</div>
                            <div><strong>{{ t.name }}</strong><span>{{ t.role }}</span></div>
                        </div>
                        <p>"{{ t.text }}"</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- FAQ -->
        <section id="faq" class="section bg-white">
            <div class="container container--narrow">
                <div class="section-header">
                    <h2>Perguntas Frequentes</h2>
                    <p>Tire suas dúvidas sobre o Imedto</p>
                </div>
                <div class="faq-list">
                    <details v-for="(item, i) in faqItems" :key="item.q" class="faq-item reveal" :style="{ transitionDelay: `${i * 60}ms` }">
                        <summary>{{ item.q }} <i class="fa-solid fa-chevron-down"></i></summary>
                        <p>{{ item.a }}</p>
                    </details>
                </div>
            </div>
        </section>

        <!-- Pricing -->
        <section id="pricing" class="section bg-white">
            <div class="container">
                <div class="section-header reveal">
                    <h2>Planos para todo tipo de clínica</h2>
                    <p>Comece grátis por 15 dias com acesso completo. Sem cartão de crédito.</p>
                </div>
                <div class="pricing-grid">
                    <div v-for="(p, i) in planos" :key="p.nome" class="pricing-card reveal" :class="{ 'pricing-card--destaque': p.destaque }" :style="{ transitionDelay: `${i * 80}ms` }">
                        <div v-if="p.destaque" class="pricing-badge">Mais popular</div>
                        <h3>{{ p.nome }}</h3>
                        <div class="pricing-price">
                            <span class="price">R${{ p.preco }}</span>
                            <span class="price-per">/mês</span>
                        </div>
                        <p class="pricing-limite">Até {{ p.limite }} profissional{{ p.limite !== 1 ? 'is' : '' }}</p>
                        <ul class="pricing-features">
                            <li v-for="f in p.features" :key="f">
                                <i class="fa-solid fa-check"></i>{{ f }}
                            </li>
                        </ul>
                        <button :class="['btn-plan', p.destaque ? 'btn-plan--primary' : 'btn-plan--secondary']" @click="irParaLogin">Começar grátis</button>
                    </div>
                </div>
                <div class="pricing-enterprise reveal">
                    <h3>Precisa de mais de 30 profissionais?</h3>
                    <p>Entre em contato para uma proposta personalizada para clínicas e hospitais de maior porte.</p>
                    <button class="btn-outline" @click="irParaLogin">Falar com a equipe Imedto</button>
                </div>
            </div>
        </section>

        <!-- CTA -->
        <section class="section cta-section">
            <div class="container cta-inner reveal">
                <h2>Pronto para modernizar sua clínica?</h2>
                <p>Junte-se a centenas de profissionais que já transformaram sua gestão médica</p>
                <button class="btn-cta" @click="irParaLogin">Começar agora gratuitamente</button>
                <span>Sem compromisso. Cancele quando quiser.</span>
            </div>
        </section>

        <!-- Footer -->
        <footer class="footer">
            <div class="container footer-grid">
                <div>
                    <img :src="logoBranco" alt="Imedto" class="footer-logo" />
                    <p>A plataforma completa para gestão médica moderna e eficiente.</p>
                </div>
                <div>
                    <h4>Produto</h4>
                    <ul>
                        <li><a href="#features">Funcionalidades</a></li>
                        <li><a href="#benefits">Benefícios</a></li>
                        <li><a href="#pricing">Planos e preços</a></li>
                        <li><router-link :to="{ name: 'Novidades' }">Novidades</router-link></li>
                    </ul>
                </div>
                <div>
                    <h4>Suporte</h4>
                    <ul>
                        <li><a href="#faq">FAQ</a></li>
                        <li><a href="#">Contato</a></li>
                        <li><router-link :to="{ name: 'Status' }">Status do sistema</router-link></li>
                    </ul>
                </div>
                <div>
                    <h4>Legal</h4>
                    <ul>
                        <li><router-link to="/termos">Termos de Uso</router-link></li>
                        <li><router-link to="/privacidade">Política de Privacidade</router-link></li>
                    </ul>
                </div>
            </div>
            <div class="footer-bottom">
                <p>© 2025 Imedto. Todos os direitos reservados.</p>
            </div>
        </footer>
    </div>
</template>

<style scoped>
.landing { font-family: 'Nunito', sans-serif; color: var(--text); }
.container { max-width: 1152px; margin: 0 auto; padding: 0 1.5rem; }
.container--narrow { max-width: 800px; }
.bg-white { background: hsl(var(--card)); }
.bg-light  { background: hsl(var(--primary-light)); }

/* Navbar */
.navbar {
    position: fixed; top: 0; left: 0; right: 0; z-index: 100;
    background: rgba(255,255,255,0.96);
    backdrop-filter: blur(6px);
    box-shadow: 0 1px 3px rgba(0,0,0,0.06);
    animation: slideDown 0.5s ease-out both;
}
.nav-inner { display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.5rem; }
.nav-logo { height: 36px; }
.nav-links { display: none; list-style: none; gap: 2rem; margin: 0; padding: 0; }
.nav-links a { color: var(--text-muted); font-weight: 600; font-size: 0.875em; transition: color 0.15s; }
.nav-links a:hover { color: hsl(var(--primary)); }
@media (min-width: 768px) { .nav-links { display: flex; } }

/* Buttons */
.btn-primary {
    background: hsl(var(--primary)); color: #fff; border: none;
    border-radius: var(--radius); font-weight: 700; font-size: 0.9em;
    padding: 0.55rem 1.1rem; cursor: pointer; font-family: inherit;
    transition: background 0.15s, transform 0.15s;
}
.btn-primary:hover { background: var(--primary-hover); transform: scale(1.03); }
.btn-lg { padding: 0.75rem 1.75rem; font-size: 1em; border-radius: var(--radius-lg); }
.btn-outline {
    display: inline-flex; align-items: center; justify-content: center;
    border: 2px solid hsl(var(--primary)); color: hsl(var(--primary));
    border-radius: var(--radius-lg); font-weight: 700; font-size: 0.9em;
    padding: 0.55rem 1.1rem; cursor: pointer; background: none; font-family: inherit;
    transition: background 0.15s, color 0.15s, transform 0.15s;
}
.btn-outline:hover { background: hsl(var(--primary)); color: #fff; transform: scale(1.03); }

/* Hero */
.hero {
    padding: 7rem 1.5rem 4rem;
    background: linear-gradient(135deg, hsl(var(--primary-light)) 0%, #ffffff 100%);
    overflow: hidden;
}
.hero-inner { display: grid; gap: 3rem; align-items: center; }
@media (min-width: 768px) { .hero-inner { grid-template-columns: 1fr 1fr; } }
.hero-texto h1 { font-size: clamp(2rem, 4vw, 3.25rem); font-weight: 800; color: hsl(var(--primary-dark)); line-height: 1.15; margin: 0 0 1rem; }
.hero-texto p  { font-size: 1.1em; color: var(--text-muted); margin: 0 0 1.5rem; }
.hero-btns { display: flex; gap: 1rem; flex-wrap: wrap; margin-bottom: 1rem; }
.hero-nota { font-size: 0.82em; color: var(--text-faint); }
.hero-card { display: flex; justify-content: center; }
.hero-card-inner { background: hsl(var(--primary-light)); border-radius: 1.25rem; box-shadow: var(--shadow-md); padding: 1.25rem; width: 100%; max-width: 320px; }
.hero-card-file { background: hsl(var(--card)); border-radius: var(--radius); padding: 1rem; margin-bottom: 0.75rem; }
.hero-card-file-header { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.75rem; }
.hc-avatar { width: 40px; height: 40px; background: rgba(69,43,151,0.12); border-radius: 50%; display: flex; align-items: center; justify-content: center; color: hsl(var(--primary)); font-size: 1.1rem; }
.hc-lines { flex: 1; display: flex; flex-direction: column; gap: 5px; }
.hc-lines span { display: block; height: 8px; background: rgba(0,0,0,0.08); border-radius: 4px; }
.hc-lines span:last-child { width: 60%; }
.hc-body { display: flex; flex-direction: column; gap: 5px; }
.hc-body span { display: block; height: 7px; background: rgba(0,0,0,0.06); border-radius: 4px; }
.hc-body span:nth-child(2) { width: 85%; }
.hc-body span:nth-child(3) { width: 70%; }
.hero-card-badges { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.hc-badge { border-radius: var(--radius); padding: 0.65rem; display: flex; flex-direction: column; align-items: center; gap: 0.25rem; font-size: 0.75em; text-align: center; }
.hc-badge i { font-size: 1.2em; }
.hc-badge.success { background: rgba(5,150,105,0.1); color: hsl(var(--success)); }
.hc-badge.info    { background: rgba(14,169,233,0.1); color: hsl(var(--info)); }

/* Sections */
.section { padding: 5rem 1.5rem; }
.section-header { text-align: center; margin-bottom: 3rem; }
.section-header h2 { font-size: clamp(1.75rem, 3vw, 2.5rem); font-weight: 800; color: hsl(var(--primary-dark)); margin: 0 0 0.75rem; }
.section-header p  { font-size: 1.05em; color: var(--text-muted); margin: 0 auto; max-width: 560px; }

/* Grids */
.grid-3 { display: grid; gap: 1.5rem; }
.grid-4 { display: grid; gap: 1.5rem; }
@media (min-width: 640px)  { .grid-4 { grid-template-columns: 1fr 1fr; } }
@media (min-width: 768px)  { .grid-3 { grid-template-columns: repeat(3, 1fr); } }
@media (min-width: 1024px) { .grid-4 { grid-template-columns: repeat(4, 1fr); } }

/* Cards */
.card { background: hsl(var(--primary-light)); border-radius: var(--radius-lg); padding: 1.5rem; transition: box-shadow 0.2s, transform 0.2s; }
.card:hover { box-shadow: 0 12px 40px -10px rgba(69,43,151,0.18); transform: translateY(-4px); }
.card-icon { width: 54px; height: 54px; background: rgba(69,43,151,0.1); border-radius: var(--radius); display: flex; align-items: center; justify-content: center; margin-bottom: 1rem; font-size: 1.45rem; color: hsl(var(--primary)); }
.card h3 { font-size: 1em; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0 0 0.5rem; }
.card p  { font-size: 0.875em; color: var(--text-muted); margin: 0; line-height: 1.6; }

/* Benefits */
.benefit-card { background: hsl(var(--card)); border-radius: var(--radius-lg); padding: 1.5rem; text-align: center; transition: box-shadow 0.2s, transform 0.2s; }
.benefit-card:hover { box-shadow: var(--shadow-md); transform: scale(1.03); }
.benefit-stat  { font-size: 2.75rem; font-weight: 800; color: hsl(var(--primary)); margin-bottom: 0.2rem; }
.benefit-label { font-size: 0.78em; font-weight: 700; color: hsl(var(--primary)); margin-bottom: 0.9rem; }
.benefit-card h3 { font-size: 0.95em; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0 0 0.4rem; }
.benefit-card p  { font-size: 0.82em; color: var(--text-muted); margin: 0; }

/* Steps */
.step-card { background: hsl(var(--primary-light)); border-radius: var(--radius-lg); padding: 2rem; text-align: center; transition: box-shadow 0.2s, transform 0.2s; }
.step-card:hover { box-shadow: var(--shadow-md); transform: scale(1.03); }
.step-num { width: 64px; height: 64px; border-radius: 50%; background: hsl(var(--primary)); color: #fff; font-size: 1.5rem; font-weight: 800; display: flex; align-items: center; justify-content: center; margin: 0 auto 1rem; }
.step-card h3 { font-size: 1.05em; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0 0 0.5rem; }
.step-card p  { font-size: 0.875em; color: var(--text-muted); margin: 0; }

/* Testimonials */
.testimonial { background: hsl(var(--card)); border-radius: var(--radius-lg); padding: 1.5rem; transition: box-shadow 0.2s, transform 0.2s; }
.testimonial:hover { box-shadow: var(--shadow-md); transform: scale(1.02); }
.testimonial-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1rem; }
.t-avatar { width: 48px; height: 48px; border-radius: 50%; background: hsl(var(--primary)); color: #fff; font-weight: 700; display: flex; align-items: center; justify-content: center; font-size: 0.95em; flex-shrink: 0; }
.testimonial-header strong { display: block; font-size: 0.88em; color: hsl(var(--primary-dark)); }
.testimonial-header span  { display: block; font-size: 0.78em; color: var(--text-muted); }
.testimonial p { font-size: 0.875em; color: var(--text-muted); margin: 0; font-style: italic; line-height: 1.6; }

/* FAQ */
.faq-list { display: flex; flex-direction: column; gap: 1rem; }
.faq-item { background: hsl(var(--primary-light)); border-radius: var(--radius-lg); overflow: hidden; transition: box-shadow 0.2s; }
.faq-item:hover { box-shadow: var(--shadow); }
.faq-item summary { padding: 1.2rem 1.5rem; font-weight: 700; font-size: 0.95em; color: hsl(var(--primary-dark)); cursor: pointer; display: flex; justify-content: space-between; align-items: center; list-style: none; user-select: none; }
.faq-item summary i { color: hsl(var(--primary)); transition: transform 0.25s; flex-shrink: 0; margin-left: 1rem; }
.faq-item[open] summary i { transform: rotate(180deg); }
.faq-item p { padding: 0 1.5rem 1.25rem; font-size: 0.875em; color: var(--text-muted); margin: 0; line-height: 1.7; }

/* Pricing */
.pricing-grid { display: grid; gap: 1.5rem; margin-bottom: 2rem; }
@media (min-width: 768px) { .pricing-grid { grid-template-columns: repeat(3, 1fr); } }
.pricing-card { background: hsl(var(--primary-light)); border: 2px solid hsl(var(--border)); border-radius: 1.25rem; padding: 1.75rem; display: flex; flex-direction: column; position: relative; transition: box-shadow 0.2s, transform 0.2s; }
.pricing-card:hover { transform: translateY(-3px); box-shadow: var(--shadow-md); }
.pricing-card--destaque { border-color: hsl(var(--primary)); box-shadow: 0 0 0 1px hsl(var(--primary)); }
@media (min-width: 768px) { .pricing-card--destaque { margin-top: -0.5rem; margin-bottom: 0.5rem; } }
.pricing-badge { position: absolute; top: -12px; left: 50%; transform: translateX(-50%); background: hsl(var(--primary)); color: #fff; font-size: 0.72em; font-weight: 700; padding: 0.25rem 0.9rem; border-radius: 99px; white-space: nowrap; }
.pricing-card h3 { font-size: 1.1em; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0 0 0.75rem; }
.pricing-price { display: flex; align-items: baseline; gap: 0.25rem; margin-bottom: 0.25rem; }
.price { font-size: 2.5rem; font-weight: 800; color: hsl(var(--primary-dark)); }
.price-per { font-size: 0.85em; color: var(--text-muted); }
.pricing-limite { font-size: 0.78em; color: var(--text-muted); margin: 0 0 1.25rem; }
.pricing-features { list-style: none; padding: 0; margin: 0 0 1.5rem; display: flex; flex-direction: column; gap: 0.6rem; flex: 1; }
.pricing-features li { display: flex; align-items: center; gap: 0.5rem; font-size: 0.875em; color: var(--text-muted); }
.pricing-features li i { color: hsl(var(--success)); font-size: 0.8em; flex-shrink: 0; }
.btn-plan { width: 100%; padding: 0.65rem; border-radius: var(--radius-lg); font-size: 0.9em; font-weight: 700; cursor: pointer; border: none; font-family: inherit; transition: transform 0.15s; }
.btn-plan:hover { transform: scale(1.03); }
.btn-plan--primary  { background: hsl(var(--primary)); color: #fff; }
.btn-plan--secondary { background: var(--text); color: #fff; }
.pricing-enterprise { background: hsl(var(--primary-light)); border: 1px solid hsl(var(--border)); border-radius: var(--radius-lg); padding: 1.5rem; text-align: center; }
.pricing-enterprise h3 { font-size: 1em; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0 0 0.5rem; }
.pricing-enterprise p  { font-size: 0.875em; color: var(--text-muted); margin: 0 0 1rem; }

/* CTA */
.cta-section { background: linear-gradient(135deg, hsl(var(--primary)) 0%, hsl(var(--primary-dark)) 100%); overflow: hidden; }
.cta-inner { text-align: center; color: #fff; display: flex; flex-direction: column; align-items: center; gap: 0.75rem; }
.cta-inner h2 { font-size: clamp(1.75rem, 3vw, 2.5rem); font-weight: 800; margin: 0; }
.cta-inner p  { font-size: 1.05em; opacity: 0.9; margin: 0; }
.cta-inner span { font-size: 0.82em; opacity: 0.75; }
.btn-cta { background: hsl(var(--card)); color: hsl(var(--primary)); border: none; border-radius: var(--radius-lg); font-size: 1em; font-weight: 700; padding: 0.85rem 2rem; cursor: pointer; font-family: inherit; transition: transform 0.2s, box-shadow 0.2s; margin: 0.5rem 0; }
.btn-cta:hover { transform: scale(1.05); box-shadow: 0 8px 30px rgba(0,0,0,0.2); }

/* Footer */
.footer { background: hsl(var(--primary-dark)); color: rgba(255,255,255,0.8); padding: 3.5rem 1.5rem 0; }
.footer-grid { display: grid; gap: 2rem; margin-bottom: 2.5rem; }
@media (min-width: 768px) { .footer-grid { grid-template-columns: 2fr 1fr 1fr 1fr; } }
.footer-logo { height: 34px; margin-bottom: 0.75rem; filter: brightness(0) invert(1); }
.footer p { font-size: 0.83em; margin: 0; line-height: 1.6; }
.footer h4 { font-size: 0.875em; font-weight: 700; color: #fff; margin: 0 0 0.875rem; }
.footer ul { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.5rem; }
.footer a  { color: rgba(255,255,255,0.7); font-size: 0.83em; transition: color 0.15s; }
.footer a:hover { color: #fff; }
.footer-bottom { border-top: 1px solid rgba(255,255,255,0.15); padding: 1.25rem 0; text-align: center; font-size: 0.82em; }

/* Animations */
@keyframes slideDown { from { opacity:0; transform:translateY(-16px); } to { opacity:1; transform:translateY(0); } }
.reveal { opacity:0; transform:translateY(24px); transition: opacity 0.55s ease-out, transform 0.55s ease-out; }
.reveal.visible { opacity:1; transform:translateY(0); }
</style>
