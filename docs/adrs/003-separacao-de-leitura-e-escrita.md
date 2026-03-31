# ADR 003: Separação de Leitura e Escrita (CQRS Lógico)

## Contexto
O modelo de dados otimizado para registro em alta cadência (append-only) na captação não oferece boa performance para agregações ou consultas filtradas no painel do administrador. Precisávamos entregar consultas em tempo real do lado do backoffice sem introduzir *locks* nas tabelas de escrita do PDV.

## Decisão
A solução passa por adotar o padrão CQRS em um modelo lógico (separando contextos e bancos).
A `Lancamentos.API` é desenhada com o comportamento exclusivo de um "Command", responsável por absorver as requisições financeiras o mais rápido possível.
Por outro lado, a `Consolidado.API` expõe o lado da "Query", consultando uma estrutura tabular já pré-calculada e otimizada para leitura.
A sincronização desse estado distribuído é garantida por meio de processos em background que escutam a mensageria da solução (descrito na ADR 001).

## Trade-offs

**Vantagens:**
- Tuning focado nas necessidades reais: as operações ganham *throughput* de escrita na API de lançamentos e otimização de leitura na do consolidado.
- Maleabilidade arquitetural: se houver necessidade futura de alimentar outro sistema (como uma pipeline de dados ou anti-fraude), o consumo ocorre pelos eventos já existentes, mantendo o PDV isolado.

**Pontos de Atenção:**
- Aumenta a carga cognitiva da equipe e o esforço de testes, sendo necessário garantir automação robusta em cenários integrados.
- Cria uma forte subordinação ao fluxo de eventos; falhas no worker propagam uma visão defasada da realidade financeira no read model.

## Status
Aprovado