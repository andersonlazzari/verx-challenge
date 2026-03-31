# Sistema de Controle de Fluxo de Caixa

## Visão Executiva

A proposta da arquitetura é a **disponibilidade operacional é inegociável**. O uso da separação estratégica entre o domínio transacional ( com foco em resiliência) e o domínio de consolidação (com foco em alta performance), garantimos que a organização nunca perca um registro, mesmo diante de falhas parciais de infraestrutura.

A arquitetura estabelece uma base escalável e orientada a eventos, o que promove a proteção do core business e prepara o código para a evolução contínua e a rápida integração com novas ferramentas.

## Contexto de Negócio
O fluxo de caixa representa o núcleo operacional do varejo. O cenário base endereça uma falha arquitetural comum no setor: a indisponibilidade da frente de caixa (PDV) ocasionada por sobrecarga em serviços analíticos, como a consulta e consolidação de saldos.

O desafio central do projeto é garantir o isolamento do motor de lançamentos (débitos e créditos) para assegurar alta disponibilidade no registro, enquanto viabiliza o consumo de dados consolidados em tempo real. A arquitetura deve suportar picos de acesso na leitura sem onerar a instância de persistência primária e sem impactar o PDV.

## Valor de Negócio Gerado
A arquitetura foi desenhada para garantir a continuidade da operação, mesmo em cenarios de falha do sistema.

Benefícios:

- **Zero perda de vendas**: O módulo de lançamentos opera de forma autônoma e resiliente.
- **Escalabilidade sob demanda**: O sistema de consolidação absorve cargas massivas de leitura sem impactar o core transacional.
- **Resiliência operacional**: Falhas em serviços periféricos não propagam degradação para a operação principal.
- **Base para crescimento**: A separação por domínios permite evolução para novos relatórios e integrações

## Domínios e Capacidades de Negócio
A solução foi desenhada utilizando conceitos de *Domain-Driven Design* (DDD) para alinhar as necessidades do negócio.

**Core Domain:** Gestão Financeira de DPV.

| Contexto | Capacidade | Valor para o Negócio |
|----------|----------|----------------------|
| PDV | Registrar transações | Garante que nenhuma venda seja perdida, mesmo sob falhas |
| Consolidação | Apurar saldo diário | Permite tomada de decisão rápida e visão financeira |

## Trade-offs Arquiteturais

- **Consistência eventual no consolidado**
  - Benefício: Isolamento de falhas e absorção de picos de carga.
  - Impacto: Introdução de retardo na propagação do saldo (aceitável dado o contexto do negócio).

- **Uso de fila (RabbitMQ)**
  - Benefício: Desacoplamento e tolerância a falhas
  - Impacto: Maior complexidade para desenvolver e manter

- **Cache em memória**
  - Benefício: Redução expressiva de latência e de custos operacionais com infraestrutura.
  - Impacto: Risco de assimetria temporária entre instâncias no caso de provisionamento horizontal.

## Atendimento aos Requisitos Não Funcionais

- **Alta disponibilidade (Lançamentos)**:
  - Arquitetura desacoplada garantindo o funcionamento independente

- **50 req/s no consolidado**:
  - Cache em memória com leitura otimizada

- **Tolerância a falhas (> 5%)**:
  - Processamento assíncrono com uso de fila
  - Retry com backoff e DLQ para tratamento de falhas

## Visão de Custos e FinOps (Azure)

A escolha de componentes seguiu uma estratégia de baixo TCO. Para suportar as 50 req/s, foi adotado o cache em memória, reduzindo o custo e a complexidade de manter um cluster mais complexo, permitindo um custo inicial estimado em **~US$ 190.00/mês** usando Azure (AKS + PostgreSQL + Service Bus).


## Segurança

- Autenticação via OAuth2/JWT
- Criptografia (HTTPS/TLS)
- Processamento idempotente para evitar duplicidade quando há retry ou reentrega de mensagens
- Logs auditáveis para rastreabilidade financeira

## Desenho da Solução (C4 Model)

Para facilitar o entendimento entre as áreas técnicas e de negócio, a arquitetura foi documentada em niveis.

### Nível 1: Contexto do Sistema
![Diagrama de Contexto](./docs/c4-model/c4-contexto-do-sistema.png)

### Nível 2: Containers
![Diagrama de Containers](./docs/c4-model/c4-containers.png)

## Decisões Arquiteturais (ADRs)
Para atender aos requisitos, algumas escolhas estratégicas foram tomadas:

 - [**Arquitetura Orientada a Eventos (EDA)**](./docs/adrs/001-arquitetura-eda.md): A API de Lançamentos faz a comunicação com o consolidado via AMQP (Fila). Garantindo que, se o consolidado cair, o PDV continua registrando o fluxo normalmente. 

 - [**Estratégia de Cache em Memória**](./docs/adrs/002-estrategia-de-cache.md): Para suportar 50 requisições por segundo na consulta de saldo, foi adotado o uso de cache em memória com TTL baixo. Com isso evitamos chamadas excessivas ao banco de dados e reduzimos a complexidade de ter um cluster de cache distribuído, reduzindo os custos e infraestrutura.

 - [**CQRS (Command Query Responsibility Segregation) Lógico**](./docs/adrs/003-separacao-de-leitura-e-escrita.md): Separação clara da modificação de estado (Lançamentos do PDV) e a consulta de saldos (Consolidado).

## Monitoramento e Observabilidade (Roadmap)
Para garantir a saúde do sistema em ambiente produtivo, a arquitetura está preparada para receber as seguintes ferramentas de telemetria:

 - **Tracing Distribuído**: Implementação de OpenTelemetry para rastrear a requisição desde o API Gateway, passando pelo RabbitMQ até o Worker.

 - **Logging Estruturado**: Utilização do Serilog para enviar logs em formato JSON para um stack ELK (Elasticsearch, Logstash, Kibana) ou Azure Application Insights.

 - **Métricas e Health Checks**: Utilização do pacote nativo Microsoft.Extensions.Diagnostics.HealthChecks exposto para o Prometheus/Grafana e para os liveness probes do Kubernetes.

## Arquitetura de Transição (Migração)
Caso esta arquitetura vise substituir um monolito legado, a adoção do padrão Strangler Fig garante a transição contínua.

A estratégia envolveria a implantação das bases de dados em paralelo, seguida de uma carga inicial (sincronização de estado). Um API Gateway assumiria o papel de roteador, direcionando o tráfego do PDV gradativamente para os novos microsserviços de lançamentos, enquanto o monolito segue respondendo pelas capacidades não migradas. A desativação do módulo correspondente no legado ocorreria apenas após rigorosa validação da integridade contábil no novo consolidador, bloqueando o risco de perdas financeiras.

## Como Executar o Projeto Localmente
Foi utilizado o Docker para facilitar a execução sem a necessidade de instalar dependências no host e facilitar o uso de tecnologias Cloud ou On-Premise.

**Pré-requisitos:** * Docker e Docker Compose instalados.
* Node.js (versão 18 ou superior) e NPM instalados (exigência apenas para rodar a interface web).

### 1. Subir o Backend

```sh
git clone [https://github.com/andersonlazzari/verx-challenge.git](https://github.com/andersonlazzari/verx-challenge.git)

cd verx-challenge

docker-compose up -d --build
```

### 2. Subir o Frontend

```sh
cd fluxo-caixa-ui
npm install
npm start
```

### 3. Acessar a Aplicação

- **Frontend**: http://localhost:4200
- **Swagger (API de Lançamentos)**: http://localhost:5000/swagger
- **Swagger (API de Consolidado)**: http://localhost:5001/swagger
- **RabbitMQ Management**: http://localhost:15672 (usuário: guest, senha: guest)

## Alternativas Tecnológicas Consideradas

A arquitetura proposta não é dependente de tecnologia e poderia ser implementada com outras stacks, como Node.js (NestJS), que oferece vantagens em cenários orientados a eventos e I/O intensivo.

No entanto, optou-se por .NET devido a stack principal da organização, reduzindo complexidade operacional e acelerando o processo de desenvolvimento e entrega.

## Roadmap
Visando um ambiente produtivo robusto, as seguintes evoluções são recomendadas:

 - **Observabilidade**: Implementação de de telemetria e tracing.

 - **Infraestrutura como Código (IaC)**: Criação de scripts para provisionamento dos recursos em nuvem.

 - **Testes de Carga:** Automação de testes com k6 para validação da meta de < 5% perda de requisições sob cargas elevadas.

 - **Resiliência avançada**: Implementação de Circuit Breaker e Retry Pattern

 - **Escalabilidade**: Evolução para cache distribuído para cenários de alta carga
