# PRD — Traços 3D Studio

**Product Requirements Document**

| Campo | Valor |
|-------|-------|
| **Produto** | Traços 3D Studio |
| **Versão do documento** | 2.0 |
| **Data** | 01/07/2026 |
| **Status** | **V1+V2 entregues** · **Trilha V3 ativa** — [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md) |
| **Referência de mercado** | [Promob Software Solutions](https://promob.com/) (linha Promob Plus como north star inicial) |
| **Stack** | C# · .NET 10 · WPF · XAML · OpenGL (OpenTK) · Windows Desktop |

---

## 1. Resumo executivo

O **Traços 3D Studio** é um software desktop para **projeto, apresentação e orçamento de móveis planejados**, voltado a marcenarias, lojistas e projetistas do setor moveleiro brasileiro. O produto conecta desenho de ambientes em 3D, biblioteca modular paramétrica e geração de informações comerciais — com evolução futura para detalhamento técnico e produção (plano de corte, CNC).

A V1–V2 concentram-se em replicar o **núcleo do Promob Plus** offline: ambiente, módulos, materiais, orçamento, detalhamento, plano de corte e export CNC Jaraguá (E.4). A **V3** (desde 01/07/2026) persegue **paridade completa** com o Promob Plus — ver [ESCOPO-V3](./ESCOPO-V3-PROMOB-COMPLETO.md).

**Estado atual (01/07/2026):** fluxo ponta a ponta operacional; **398** testes; build `2026.06.26.2012`; trilha Promob blocos A–E + releases V2.1–V2.8 ✅.

---

## 2. Visão e missão

### 2.1 Visão (3–5 anos)

Ser a alternativa brasileira acessível e eficiente para projetar, apresentar e orçar ambientes de móveis planejados — do traço inicial à lista de peças para a marcenaria.

### 2.2 Missão

Entregar uma ferramenta **intuitiva, precisa (mm) e produtiva** que reduza retrabalho entre projetista, vendedor e produção.

### 2.3 Princípios de produto

1. **Unidade em milímetros** — padrão do mercado moveleiro BR.
2. **Projeto antes de produção** — ambiente e módulos sólidos antes de CNC.
3. **Parametria com regras** — dimensões dentro de limites construtivos.
4. **Fluxo Promob-like** — biblioteca à esquerda, viewport central, propriedades à direita.
5. **Evolução incremental** — cada fase entrega valor utilizável.

---

## 3. Público-alvo e personas

### 3.1 Segmentos

| Segmento | Necessidade principal |
|----------|----------------------|
| Marcenaria sob medida | Projetar cozinhas/dormitórios e gerar orçamento |
| Loja de móveis planejados | Apresentar 3D ao cliente e fechar venda |
| Projetista autônomo | Agilidade no desenho e documentação |
| Fábrica pequena/média (fase posterior) | Lista de corte e padronização |

### 3.2 Personas

**Persona A — Carla, projetista de cozinhas (32 anos)**  
- Usa software moveleiro diariamente.  
- Precisa: desenhar ambiente rápido, inserir módulos da biblioteca, ajustar medidas, exportar imagem/PDF para o cliente.  
- Frustração: software caro, curva de aprendizado longa.

**Persona B — Roberto, dono de marcenaria (45 anos)**  
- Vende e supervisiona produção.  
- Precisa: orçamento automático, lista de peças, menos erro na fábrica.  
- Frustração: projeto bonito que não vira corte correto.

**Persona C — Ana, vendedora de loja (28 anos)**  
- Apresenta projetos a clientes finais.  
- Precisa: visual 3D claro, alterar cores/materiais, proposta rápida.  
- Frustração: depender sempre do projetista para mudanças simples.

---

## 4. Referência competitiva

### 4.1 Promob — o que espelhamos

| Módulo Promob Plus | Prioridade Traços 3D | Fase |
|--------------------|----------------------|------|
| Construção do ambiente | Alta | 1 |
| Movimentação 3D | Alta | 1 |
| Aberturas (porta/janela) | Alta | 1 |
| Construtor de armários / biblioteca | Alta | 2 ✅ flat · **V3.7** regras |
| Configurador de dimensões | Alta | 2 |
| Materiais e ambientação | Média | 3 |
| Detalhamento técnico 2D | Média | 4 |
| Orçamento | Alta | 3 |
| Render / apresentação | Média | 3 |
| Produção / Cut / CNC | Alta | 5 + **V2.8 E.4** |
| ERP / gestão de lojas | V3.5 (escala) | 6 |

### 4.2 Diferenciação pretendida

- Foco no fluxo essencial (projeto → orçamento) sem sobrecarga de módulos corporativos.
- Interface limpa, inspirada no layout já implementado.
- Caminho aberto para bibliotecas e regras customizadas da marcenaria.
- Stack moderna (.NET 10, WPF) e manutenível.

---

## 5. Escopo do produto

### 5.1 Dentro do escopo (V1 → V3)

- Aplicativo Windows desktop instalável.
- Projeto 3D de ambientes fechados (paredes, piso, aberturas, teto básico).
- Biblioteca de módulos paramétricos (cozinha, dormitório, painéis; **V3:** mais ambientes).
- Edição de dimensões, posição, materiais, camadas, faixas, regiões.
- Persistência `.tracos` + biblioteca `.tracos-lib`.
- Orçamento, PDF comercial, detalhamento 2D, plano de corte MaxRects.
- Export CNC: JSON/CSV/`tracos-cnc-job`/ **`.tap` Jaraguá Mach4** (E.4).
- **V3:** paridade UX (P8, F7, S3), polish fábrica (adiado), catálogo expandido, **engenharia de modulação (V3.7)**, intercâmbio `.promob`/SKP, Connect/nuvem/ERP.

### 5.2 Fora do escopo imediato (V3.5+ ou decisão comercial)

- Versão web ou mobile.
- Render IA fotorealista / VR (prioridade baixa V3.3+).
- Marketplace de fornecedores Promob.
- Licenciamento (D5) — até decisão comercial (**V3.6a**).

---

## 6. Jornadas do usuário

### 6.1 Jornada principal — Novo projeto de cozinha

```
[Novo projeto] → [Desenhar paredes] → [Fechar ambiente]
      → [Inserir módulos da biblioteca] → [Ajustar dimensões/materiais]
      → [Inserir porta/janela] → [Visualizar 3D / planta]
      → [Gerar orçamento] → [Exportar PDF/imagem] → [Salvar]
```

### 6.2 Jornada secundária — Editar projeto existente

```
[Abrir] → [Selecionar parede ou módulo] → [Editar no painel Propriedades]
      → [Salvar] → [Atualizar orçamento]
```

### 6.3 Jornada produção — Enviar para fábrica

```
[Projeto aprovado] → [Lista de peças] → [Plano de corte MaxRects]
      → [Export JSON/CSV/tracos-cnc-job] → [Export .tap Jaraguá Mach4 (E.4)]
      → [Validação na chapa] → (V3.2 polish nesting vs Aspire)
```

---

## 7. Requisitos funcionais

Legenda: **P0** bloqueante · **P1** importante · **P2** desejável · **Status:** ✅ Feito · 🟡 parcial · ⬜ V3

### 7.1 Fase 1 — Núcleo de ambiente ✅

| ID | Requisito | P | Status |
|----|-----------|---|--------|
| RF-1.01 | Desenhar paredes por pontos com snap 100 mm | P0 | ✅ |
| RF-1.02 | Snap de ângulo 45° | P0 | ✅ |
| RF-1.03 | Entrada numérica de comprimento (mm) | P0 | ✅ |
| RF-1.04 | Orientação Interna/Externa (tecla R) | P0 | ✅ |
| RF-1.05 | Fechamento automático de ambiente | P0 | ✅ |
| RF-1.06 | Preview de parede (linha fantasma) | P0 | ✅ |
| RF-1.07 | Seleção de parede por clique | P0 | ✅ |
| RF-1.08 | Edição de comprimento de parede selecionada | P0 | ✅ |
| RF-1.09 | Exclusão de parede (Delete) | P0 | ✅ |
| RF-1.10 | Desfazer (Ctrl+Z) | P0 | ✅ |
| RF-1.11 | Câmera perspectiva: zoom, pan, órbita | P0 | ✅ |
| RF-1.12 | Vista ortográfica no modo parede | P0 | ✅ |
| RF-1.13 | Inserir porta em parede | P0 | ✅ |
| RF-1.14 | Inserir janela com peitoril | P0 | ✅ |
| RF-1.15 | Recorte visual da parede nas aberturas | P1 | ✅ |
| RF-1.16 | Piso automático ao fechar ambiente | P1 | ✅ |
| RF-1.17 | Vistas Perspectiva, Planta, Frontal, Esquerda, Direita, Raio X | P1 | ✅ |
| RF-1.18 | Novo / Abrir / Salvar `.tracos` | P0 | ✅ |
| RF-1.19 | Barra de status (face, seleção, material) | P2 | ✅ |
| RF-1.20 | Modelo único `WallSegment` (sem `Wall` legado) | P1 | ✅ |
| RF-1.21 | Diálogo confirmar fechamento de parede (P8 Promob) | P2 | ✅ **V3.1a** |
| RF-1.22 | Editar Regiões no editor de faixas (F7 Promob) | P2 | ✅ **V3.1b** |

### 7.2 Fase 2 — Biblioteca e módulos ✅ + V2

| ID | Requisito | P | Status |
|----|-----------|---|--------|
| RF-2.01 | Modelo `Module` paramétrico (L × A × P) | P0 | ✅ |
| RF-2.02 | Inserir módulo da biblioteca no 3D | P0 | ✅ |
| RF-2.03 | Balcão 2 portas | P0 | ✅ |
| RF-2.04 | Balcão 3 portas | P1 | ✅ |
| RF-2.05 | Gaveteiro | P1 | ✅ |
| RF-2.06 | Aéreo | P1 | ✅ |
| RF-2.07 | Seleção de módulo por clique | P0 | ✅ |
| RF-2.08 | Painel Propriedades do módulo | P0 | ✅ |
| RF-2.09 | Limites min/max por tipo | P0 | ✅ |
| RF-2.10 | Snap em parede e entre módulos | P1 | ✅ |
| RF-2.11 | Mover módulo | P0 | ✅ |
| RF-2.12 | Rotacionar 90° | P1 | ✅ |
| RF-2.13 | Excluir módulo | P0 | ✅ |
| RF-2.14 | Colisão ON/OFF | P2 | ✅ |
| RF-2.15 | Dormitórios + Painéis (L7) | P2 | ✅ |
| RF-2.16 | Abas Inserir / Ambiente + lista (B.2) | P1 | ✅ |
| RF-2.17 | Agrupar lista por cômodo/parede (A3/A3c) | P1 | ✅ V2.7 |
| RF-2.18 | Visível/bloqueado, renomear, multi-seleção (A4/A5/A8) | P1 | ✅ V2 |
| RF-2.19 | Recarregar biblioteca `.tracos-lib` (L10) | P1 | ✅ V2.6 |

### 7.3 Fase 3 — Apresentação e orçamento ✅

| ID | Requisito | P | Status |
|----|-----------|---|--------|
| RF-3.01 | Catálogo de materiais | P0 | ✅ |
| RF-3.02 | Aplicar material (módulo, parede, faixa, região) | P1 | ✅ |
| RF-3.03 | Objetos de decoração (pia, fogão, geladeira) | P2 | ⬜ **V3.3e** |
| RF-3.04 | Exportar PNG viewport / apresentação 2× | P0 | ✅ |
| RF-3.05 | Lista de itens + orçamento | P0 | ✅ |
| RF-3.06 | Tabela de preços configurável | P0 | ✅ |
| RF-3.07 | Cálculo automático de orçamento | P0 | ✅ |
| RF-3.08 | Relatório PDF comercial | P1 | ✅ |
| RF-3.09 | Dados do cliente no projeto | P2 | ✅ |
| RF-3.10 | Abertura animada portas/gavetas | P2 | ⬜ **V3.3+** |

### 7.4 Fase 4 — Detalhamento técnico ✅

| ID | Requisito | P | Status |
|----|-----------|---|--------|
| RF-4.01 | Planta baixa 2D com cotas | P0 | ✅ |
| RF-4.02 | Vistas elevadas | P1 | ✅ |
| RF-4.03 | Lista de peças | P0 | ✅ |
| RF-4.04 | Decomposição módulo em painéis | P0 | ✅ |
| RF-4.05 | Export DXF planta e peças com furos | P1 | ✅ |
| RF-4.06 | Impressão folha técnica PDF | P2 | ✅ |

### 7.5 Fase 5 — Produção ✅ + E.4

| ID | Requisito | P | Status |
|----|-----------|---|--------|
| RF-5.01 | Espessura chapa e fita de borda | P0 | ✅ |
| RF-5.02 | Furos dobradiça e minifix | P1 | ✅ |
| RF-5.03 | Plano de corte MaxRects | P0 | ✅ |
| RF-5.04 | Export CSV / JSON máquina / `tracos-cnc-job` | P1 | ✅ E.1–E.3 |
| RF-5.05 | Etiquetas PDF | P2 | ✅ |
| RF-5.06 | Auditoria pré-orçamento | P2 | ✅ |
| RF-5.07 | Export `.tap` Jaraguá Mach4 (Solid TAF) | P1 | ✅ **E.4 V2.8** |
| RF-5.08 | Calibrar nesting TAP vs Aspire | P1 | ⏸ **V3.2a adiado** |
| RF-5.09 | Export DXF nesting no menu | P1 | ⬜ **V3.2b** |

### 7.6 Fase 6 — Escala

| ID | Requisito | P | Status |
|----|-----------|---|--------|
| RF-6.01 | Biblioteca custom `.tracos-lib` + editor | P2 | ✅ |
| RF-6.02 | Import/export catálogo e preços (arquivo) | P2 | ✅ |
| RF-6.03 | Multi-usuário / projetos nuvem | P2 | ⬜ **V3.5c** |
| RF-6.04 | API ERP tempo real | P2 | ⬜ **V3.5d** |
| RF-6.05 | Abas multi-projeto (S3) | P2 | ✅ **V3.1c** |
| RF-6.06 | Connect / catálogo online (L9) | P2 | ⬜ **V3.5b** |

### 7.7 Backlog V3 — paridade Promob Plus completa

Ver tabela completa de gates: [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md)

| Onda | Requisitos / IDs | Resumo |
|------|------------------|--------|
| **V3.1a–c** | RF-1.21, F7, RF-6.05 | P8 ✅, F7 ✅, S3 ✅ |
| **V3.2** | RF-5.08, RF-5.09 | CNC fábrica |
| **V3.3** | RF-3.03, RF-2.15+, RF-7.05 | Catálogo banheiro, lavanderia, cômodo auto |
| **V3.7** | RF-7.06–RF-7.10 | Engenharia modulação / Construtor |
| **V3.4** | RF-7.01–RF-7.03 | Import/export `.promob`, SKP |
| **V3.5** | RF-6.03–RF-6.06 | Plataforma Connect-like |
| **V3.6** | D5, smoke release | Licença + release V3 |

| ID | Requisito | P | Status |
|----|-----------|---|--------|
| RF-7.01 | Import subset `.promob` (paredes + módulo) | P1 | ⬜ V3.4 |
| RF-7.02 | Export subset `.promob` | P2 | ⬜ V3.4 |
| RF-7.03 | Import SKP (decoração ou módulo) | P2 | ⬜ V3.4 |
| RF-7.04 | Cômodo automático ao fechar ambiente | P2 | ⬜ V3.3c |
| RF-7.05 | Pacotes catálogo novos ambientes | P1 | ⬜ V3.3 |
| RF-7.06 | Spike Construtor Promob → tabela EM1–EM8 | P1 | ✅ V3.7-spike |
| RF-7.07 | Schema regras modulação em `.tracos-lib` | P0 | ✅ V3.7a |
| RF-7.08 | Editor de modulação (estrutura, vãos, divisórias, portas/gavetas) | P0 | ✅ V3.7b |
| RF-7.09 | Motor paramétrico resize → peças internas | P0 | ✅ V3.7c |
| RF-7.10 | Regras usinagem/fita configuráveis por template | P1 | ⬜ V3.7d–e |

---

## 8. Requisitos não funcionais

| ID | Requisito | Critério de aceite |
|----|-----------|-------------------|
| RNF-01 | Plataforma | Windows 10/11 x64 |
| RNF-02 | Performance | Viewport ≥ 30 FPS em ambiente típico (≤ 20 módulos) |
| RNF-03 | Precisão | Cálculos em float; exibição em mm sem casas ou 1 casa |
| RNF-04 | Persistência | Projeto salvo recupera 100% geometria e metadados |
| RNF-05 | Usabilidade | Projetista cria ambiente fechado em ≤ 5 min (treinado) |
| RNF-06 | Idioma | Interface em português (BR) |
| RNF-07 | Manutenibilidade | Separação UI / domínio / renderização |
| RNF-08 | Instalação | Build publicável (MSI ou ClickOnce futuro) |
| RNF-09 | Dados locais | Projetos em pasta do usuário; sem dependência de internet |
| RNF-10 | Licenciamento | A definir (comercial / assinatura / perpetua) |

---

## 9. Arquitetura técnica

### 9.1 Stack atual (01/07/2026)

```
┌─────────────────────────────────────────────────────────┐
│  Apresentação (WPF + XAML)                               │
│  MainWindow · TabControl biblioteca · Janelas modais     │
├─────────────────────────────────────────────────────────┤
│  Aplicação (C#)                                          │
│  Input · Câmera · Comandos · Export CNC · Orçamento      │
├─────────────────────────────────────────────────────────┤
│  Renderização (OpenTK + OpenGL 3.3 Core)                 │
│  RenderEngine · VBO/VAO · paredes · módulos · aberturas  │
├─────────────────────────────────────────────────────────┤
│  Domínio (C#)                                            │
│  Project · Room · WallSegment · ModuleInstance           │
│  RoomCompartment · materiais · camadas · regiões         │
├─────────────────────────────────────────────────────────┤
│  Infraestrutura                                          │
│  `.tracos` JSON · QuestPDF · DXF · JaraguaMach4TapExporter│
└─────────────────────────────────────────────────────────┘
```

### 9.2 Modelo de dados (implementado)

```
Project
├── Metadata (nome, cliente, data, versão)
├── Room
│   ├── WallSegment[]
│   │   └── WallOpening[]
│   └── FloorSurface
└── ModuleInstance[]
    ├── ModuleDefinitionId
    ├── Transform (posição, rotação)
    ├── Dimensions (L, A, P)
    └── Materials
```

### 9.3 Formato de arquivo `.tracos`

- JSON ou MessagePack serializando `Project`.
- Versionamento de schema (`schemaVersion`) para migrações futuras.
- Unidades sempre em mm.

### 9.4 Refatorações técnicas planejadas

| Item | Motivo | Fase |
|------|--------|------|
| Extrair `RenderEngine` de `MainWindow.xaml.cs` | Manutenção | 1 |
| Extrair `CameraController` | Reuso nas vistas | 1 |
| Remover `Wall` legado | Fonte única de verdade | 1 |
| Migrar OpenGL imediato → VBO/VAO (opcional) | Performance | 2–3 |
| Usar `MeshData` na renderização | Módulos e piso | 2 |

---

## 10. Interface do usuário

### 10.1 Layout (já implementado)

| Região | Conteúdo |
|--------|----------|
| Menu superior | Arquivo, Editar, Exibir, Inserir, Projeto, Produção, Orçamento, Ferramentas, Ajuda |
| Toolbar | Novo, Abrir, Salvar, Parede, Porta, Janela, vistas |
| Esquerda | TabControl **Inserir** / **Ambiente** + guia Materiais |
| Centro | Viewport OpenGL 3D |
| Direita | Propriedades (Dimensões, Posicionamento, Materiais, etc.) |
| Rodapé | Perfil, Colisão, Unidade mm, Face, Status |

### 10.2 Padrões de interação

| Ação | Atalho / gesto |
|------|----------------|
| Desfazer parede | Ctrl+Z |
| Cancelar modo | Esc |
| Alternar orientação parede | R |
| Excluir seleção | Delete |
| Zoom | Scroll |
| Pan | Botão do meio |
| Órbita | Meio + direito |
| Medida exata | Caixa de medida + Enter |

---

## 11. Métricas de sucesso (KPIs)

### 11.1 Produto (por fase)

| Fase | KPI | Meta |
|------|-----|------|
| 1 | Ambiente fechado + salvar/abrir | 100% dos casos de teste |
| 2 | Inserir e editar 4 tipos de módulo | ≤ 2 min por módulo |
| 3 | Orçamento automático vs manual | Erro < 2% |
| 4 | Lista de peças vs módulo real | 100% peças corretas |
| 5 | Plano de corte | Aproveitamento ≥ 75% em caso teste |

### 11.2 Negócio (pós-lançamento)

- Tempo médio de projeto de cozinha simples.
- Taxa de adoção em marcenarias piloto.
- NPS de projetistas.
- Churn de assinatura (se aplicável).

---

## 12. Riscos e mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| `MainWindow.xaml.cs` monolítico | Alto | Refatorar na Fase 1 |
| OpenGL legado limita qualidade visual | Médio | Materiais simples primeiro; evoluir pipeline depois |
| Escopo igual ao Promob completo | Alto | Fases rígidas; MVP claro |
| Regras construtivas complexas | Alto | Começar com 4 módulos e regras fixas |
| Sem persistência atrasa testes reais | Alto | RF-1.18 prioritário no fim da Fase 1 |

---

## 13. Glossário

| Termo | Definição |
|-------|-----------|
| **Módulo** | Unidade de móvel parametrizável (ex.: balcão 2 portas) |
| **Ambiente** | Conjunto de paredes fechadas formando um cômodo |
| **Orientação de parede** | Lado da referência de medida (interna/externa/centro) |
| **Plano de corte** | Disposição de peças em chapas para fabricação |
| **Nesting** | Otimização de encaixe de peças na chapa |
| **Parametria** | Dimensões controladas por variáveis e regras |

---

## 14. Aprovações e histórico

| Versão | Data | Autor | Alterações |
|--------|------|-------|------------|
| 1.0 | 16/06/2026 | Traços 3D Studio | Documento inicial |
| 2.0 | 01/07/2026 | Traços 3D Studio | V1+V2 ✅; RF atualizados; trilha V3; 398 testes |

---

## 15. Documentos relacionados

- [Plano de Execução](./PLANO-EXECUCAO.md) — fases, marcos V1/V2, trilha V3
- [Escopo V3 — Promob Plus completo](./ESCOPO-V3-PROMOB-COMPLETO.md) — **fonte da verdade backlog**
- [Engenharia modulação V3.7](./manual/modulos/08-engenharia-modulacao-construtor.md)
- [Escopo V1/V2 (histórico)](./ESCOPO-V1-VS-PROMOB.md)
- Código-fonte: `Tracos3DStudio/`
- [Promob Plus — Suporte](https://suporte.promob.com/hc/pt-br/articles/31123224474257-Plus)
