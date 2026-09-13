# FPSCore — Guia de Desenvolvimento

Documento oficial do projeto **FPSCore**. Registra o objetivo, a arquitetura, a
estrutura de pastas, o passo a passo de montagem, os scripts, o fluxo de teste,
como gerar o pacote e como importá-lo/atualizá-lo em jogos futuros.

Este guia assume a decisão já tomada: **FPSCore deixou de ser "desenvolvido
junto com um jogo"** e passa a ser um **projeto Unity próprio e independente**,
cujo único propósito é produzir e manter um pacote reutilizável de movimentação
em primeira pessoa.

---

## Sumário

1. [Objetivo e filosofia](#1-objetivo-e-filosofia)
2. [Criação do projeto Unity](#2-criação-do-projeto-unity)
3. [Estrutura de pastas](#3-estrutura-de-pastas)
4. [Cena de testes](#4-cena-de-testes)
5. [Montagem do Player](#5-montagem-do-player)
6. [Configuração do CharacterController](#6-configuração-do-charactercontroller)
7. [Scripts do FPSCore](#7-scripts-do-fpscore)
8. [Revisão de arquitetura e código](#8-revisão-de-arquitetura-e-código)
9. [PlayerMovementConfig](#9-playermovementconfig)
10. [Sistemas opcionais](#10-sistemas-opcionais)
11. [Fluxo de comunicação entre scripts](#11-fluxo-de-comunicação-entre-scripts)
12. [Sistema de Input](#12-sistema-de-input)
13. [Checklist de testes](#13-checklist-de-testes)
14. [Formato do pacote](#14-formato-do-pacote)
15. [Estrutura final do pacote](#15-estrutura-final-do-pacote)
16. [Projeto-fonte vs. pacote final](#16-projeto-fonte-vs-pacote-final)
17. [Instalar o FPSCore em um jogo novo](#17-instalar-o-fpscore-em-um-jogo-novo)
18. [Atualizar o pacote no futuro](#18-atualizar-o-pacote-no-futuro)
19. [Roadmap futuro](#19-roadmap-futuro)
20. [Checklist-mestre](#20-checklist-mestre)

---

## 1. Objetivo e filosofia

### O que é o FPSCore

FPSCore é um **pacote Unity reutilizável de movimentação e câmera em primeira
pessoa**: andar, correr, pular, olhar com o mouse, travar/destravar cursor,
mais três sistemas opcionais de polimento (áudio de passo, head bob, FOV ao
correr).

### Qual problema ele resolve

Sem ele, cada jogo novo da sua produção repetiria o mesmo trabalho: importar o
Standard Assets (ou reescrever do zero) um controller de primeira pessoa,
recalibrar os mesmos números, e carregar código específico de um jogo dentro
do controller de outro. FPSCore existe pra que esse trabalho seja feito **uma
vez, bem feito, testado isoladamente**, e depois só importado.

### Por que ele existe como projeto separado

Um projeto de jogo carrega inevitavelmente decisões daquele jogo: cenário,
iluminação, sistemas de gameplay específicos, assets pesados. Se o FPSCore
crescesse dentro de um projeto assim, duas coisas ruins acontecem:

- É fácil, sem querer, o controller acabar referenciando algo do jogo (uma UI,
  um singleton, um layer específico) — isso quebra a reutilização silenciosamente.
- Cada vez que o jogo evolui (novas cenas, assets, dependências), o "ambiente"
  em que o pacote é testado fica mais pesado e mais difícil de isolar.

Um projeto separado resolve isso estruturalmente: **não tem como o pacote
acidentalmente depender de algo de jogo, porque não existe jogo nesse
projeto.**

### O que ele NÃO é

- **Não é um jogo.** Não tem objetivo, progressão, arte final, nem sistemas de
  gameplay.
- **Não é um protótipo de um jogo específico.** Nenhuma decisão de design de
  um jogo da antologia (Fotógrafo de Vida Selvagem, ou qualquer outro) deve
  vazar pra dentro dele.
- **Não é o produto final** que o jogador vê. É a fábrica que produz uma peça
  que outros projetos vão importar.

### O que ele é

Um **projeto-fonte**: onde os scripts são escritos, testados numa cena de
playground neutra, refinados, versionados — e de onde um **pacote** (formato
UPM, ver seção 14) é gerado e consumido por quantos jogos você quiser.

```
FPSCore Development Project
        │
        ├── Desenvolvimento dos scripts
        ├── Cena de testes (playground neutro)
        ├── Configurações (ScriptableObjects)
        ├── Testes e refinamentos
        │
        ▼
   FPSCore Package  (com.mateuskogl.fpscore)
        │
        ▼
Reutilização em: Fotógrafo de Vida Selvagem, e futuros jogos da antologia
```

### Filosofia de arquitetura

Todas as decisões de código do FPSCore são subordinadas a estes princípios,
nessa ordem de prioridade:

| Princípio | O que significa na prática |
|---|---|
| **Script pequeno, uma responsabilidade** | `PlayerMotor` só move; não toca áudio, não mexe em câmera, não lê Esc. |
| **Baixo acoplamento** | Scripts se conectam por **método público** e **evento** (`SetMovementLocked`, `OnFootstep`...), nunca vasculhando o componente interno de outro. |
| **Configuração separada do código** | Nenhum número de gameplay (velocidade, altura de pulo, sensibilidade) fica hardcoded — tudo vive em `PlayerMovementConfig`, um `ScriptableObject`. Trocar de jogo é trocar de asset, não de script. |
| **Sistemas opcionais desacoplados** | `FootstepAudioPlayer`, `CameraHeadBob`, `RunFovKick` são add-ons que **escutam** o core; removê-los não quebra nada. |
| **Fácil reutilização** | Nenhum script referencia algo específico de um jogo (UI, diálogo, IA). Comunicação com sistemas de jogo é sempre o jogo se inscrevendo no core — nunca o contrário. |
| **Fácil expansão sem virar um monólito** | Uma feature nova (agachar, stamina) vira um script novo que lê o `PlayerMotor` por fora, não uma responsabilidade nova enfiada dentro dele. Ver [Roadmap](#19-roadmap-futuro). |

Essa ordem de prioridade importa: se uma otimização de performance exigisse
acoplar dois scripts, a resposta é não fazer essa otimização — estabilidade e
reutilização vêm antes de performance aqui, porque o custo de um controller
engessado é pago em **todos** os jogos futuros, não só neste.

---

## 2. Criação do projeto Unity

### Versão do Unity

Use uma versão **LTS** (Long Term Support) — nunca uma versão Tech Stream para
o projeto-fonte de um pacote que vai viver vários anos e ser consumido por
vários jogos.

- **Recomendado:** Unity **2022.3 LTS** (ou a LTS mais recente disponível no
  momento em que você criar o projeto). LTS recebe correções por mais tempo e
  é a base mais provável de já estar instalada nos seus próximos projetos.
- Se você já pretende que os próximos jogos da antologia usem uma LTS mais
  nova (ex: Unity 6 LTS), crie o FPSCore nessa mesma versão — o pacote deve
  ser igual ou mais antigo que a versão mínima dos jogos que vão consumi-lo,
  nunca mais novo (Unity não abre projetos/pacotes de uma versão futura).

O campo `"unity"` do `package.json` (seção 15) declara essa versão mínima
explicitamente.

### Tipo de projeto (template)

Use o template **3D (Core)**, sem URP nem HDRP.

Motivo: o FPSCore não renderiza nada além de usar uma `Camera` padrão — ele
não depende de render pipeline nenhum. Prender o projeto-fonte a um pipeline
específico (URP, por exemplo) não ajuda em nada o pacote e ainda cria uma
dependência de pacote da Unity (`com.unity.render-pipelines.universal`) que
o `package.json` do FPSCore não precisa declarar. Cada jogo que importar o
FPSCore vai ter seu próprio pipeline (URP, HDRP, Built-in) — o pacote
funciona igual em qualquer um, porque não mexe em shaders, materiais nem
configuração de câmera além de `fieldOfView`.

### Nome do projeto

Recomendado: **`FPSCore Development`** (nome da pasta do projeto Unity no
disco).

Motivo pra não usar só `FPSCore`: esse nome vai ser reservado pro **repositório
git do pacote** (seção 14/18) — que, mais pra frente, pode ser uma pasta ou
repositório separado dentro do próprio disco/organização. Ter o projeto Unity
e o repositório do pacote com nomes idênticos convida a confusão no
Explorer/Editor (duas janelas de Unity chamadas "FPSCore", dois ícones na
barra de tarefas). `FPSCore Development` deixa claro, só pelo nome, que é o
**ambiente de desenvolvimento**, não o produto.

### Input System

O FPSCore depende do **Input System novo** (`com.unity.inputsystem`), não do
`Input Manager` legado.

1. `Window > Package Manager > Packages: Unity Registry` → busque **Input
   System** → `Install`.
2. `Edit > Project Settings > Player > Other Settings > Active Input Handling`
   → mude para **"Input System Package (New)"**.
   - Se algum dia este projeto-fonte precisar rodar algo que só existe no
     sistema antigo (não deveria acontecer aqui), use `"Both"` em vez de
     substituir — mas para o FPSCore puro, `"Input System Package (New)"`
     sozinho é suficiente e mais limpo.
3. O Unity vai pedir para **reiniciar o Editor** depois de trocar o Active
   Input Handling — confirme o reinício antes de continuar.
4. **Não é preciso criar um `.inputactions` asset.** O `PlayerInputReader`
   monta as `InputAction`s inteiramente em código (decisão já tomada e
   revalidada na seção 12) — instalar o pacote Input System e ativá-lo já é
   suficiente para o FPSCore funcionar em qualquer projeto que o importe,
   sem nenhum asset extra pra configurar.

Esse é, propositalmente, o único pré-requisito externo do pacote. Evitar
dependências de configuração específica de projeto (Tags customizadas, Layers
com nome fixo, Input Actions asset) é parte da filosofia de reuso da seção 1.

---

## 3. Estrutura de pastas

### Decisão: o pacote vive embutido (`Packages/`) dentro do próprio projeto-fonte, desde o primeiro commit

O rascunho inicial (`Assets/FPSCore/Runtime/...`) tem um problema real: código
dentro de `Assets/` **não é** um pacote UPM válido — é só uma pasta de
scripts. Pra virar pacote de verdade (seção 14/15) ele precisaria ser movido
pra `Packages/` de qualquer forma, mais tarde, com o risco de esquecer de
mover algo ou de referências no Editor apontando pro lugar errado.

A correção: o pacote **já nasce** como pacote — vive em
`Packages/com.mateuskogl.fpscore/` dentro do próprio projeto FPSCore
Development, desde o começo. O Unity trata isso como um "embedded package"
("In Project" no Package Manager): compila normalmente, respeita o
`.asmdef`, e **é literalmente a mesma pasta** que depois vira o pacote
distribuído — sem etapa de "empacotar" ou copiar nada.

`Assets/` neste projeto passa a conter **só** o que existe para desenvolver e
testar o pacote, e que **nunca** é exportado: a cena de testes e material de
playground.

### Estrutura completa

```
FPSCore Development/                        (raiz do projeto Unity)
│
├── Assets/
│   ├── Scenes/
│   │   └── FPSCore_TestScene.unity         ← cena de testes (seção 4)
│   │
│   ├── _Sandbox/                           ← só playground: materiais,
│   │   ├── Materials/                        prefabs de blocos de teste,
│   │   └── Prefabs/                          nada disso vai pro pacote
│   │
│   └── Documentation/                      ← notas de desenvolvimento,
│       └── FPSCore_Development_Guide.md      cópia deste guia (opcional,
│                                              fica fora do build por padrão)
│
├── Packages/
│   ├── manifest.json                       ← declara com.unity.inputsystem
│   │
│   └── com.mateuskogl.fpscore/             ★ O PACOTE — ver seção 15 para o
│       │                                      detalhamento completo
│       ├── package.json
│       ├── README.md
│       ├── CHANGELOG.md
│       ├── Runtime/
│       │   ├── FPSCore.Runtime.asmdef
│       │   ├── Config/
│       │   │   └── PlayerMovementConfig.cs
│       │   └── Scripts/
│       │       ├── Input/PlayerInputReader.cs
│       │       ├── Movement/PlayerMotor.cs
│       │       ├── Camera/FirstPersonLook.cs
│       │       ├── Audio/FootstepAudioPlayer.cs
│       │       └── Effects/
│       │           ├── CameraHeadBob.cs
│       │           └── RunFovKick.cs
│       ├── Editor/                         ← reservado, vazio por enquanto
│       ├── Tests/                          ← reservado, vazio por enquanto
│       ├── Samples~/
│       │   └── DemoScene/                  ← versão exportável e mínima
│       │                                      da FPSCore_TestScene
│       └── Documentation~/
│
└── ProjectSettings/
```

### O que pertence ao pacote (`Packages/com.mateuskogl.fpscore/`)

Só o que qualquer jogo que importar o FPSCore precisa: scripts, config,
`.asmdef`, samples, documentação do pacote em si. **Nada aqui pode referenciar
algo específico deste projeto de desenvolvimento** (a cena de teste, materiais
do playground).

### O que pertence só ao projeto de desenvolvimento (`Assets/`)

- `Assets/Scenes/FPSCore_TestScene.unity` — a cena viva de testes, que você
  edita e recarrega o tempo todo durante o desenvolvimento. Fica fora do
  pacote porque ela tende a acumular objetos de teste descartáveis (cubos,
  rampas provisórias) que não fazem sentido num pacote limpo.
- `Assets/_Sandbox/` — qualquer material, textura de teste, prefab de bloco
  usado só pra montar o playground. Prefixo `_` só para ficar sempre no topo
  da listagem do Project.
- `Assets/Documentation/` — notas soltas, rascunhos, cópias de referência.
  Opcional; o pasta com til (`Documentation~`) dentro do pacote é a
  documentação oficial que viaja com ele.

### O que não deve ser exportado para os jogos

Tudo que está em `Assets/` neste projeto. A regra prática é: **se está fora de
`Packages/com.mateuskogl.fpscore/`, não é FPSCore — é ferramental de
desenvolvimento**, e não deve ser copiado, referenciado ou versionado junto
quando você distribuir o pacote (seção 16 detalha isso no controle de versão).

### Onde fica a cena de testes

`Assets/Scenes/FPSCore_TestScene.unity`, fora do pacote (justificativa acima).
Uma versão **reduzida e limpa** dela (só o essencial pra alguém importar o
pacote e já ver funcionando) vive dentro do pacote em
`Samples~/DemoScene/` — essa sim é opcionalmente importável por quem instalar
o FPSCore via Package Manager (botão "Samples" na aba do pacote).

---

## 4. Cena de testes

### Objetivo

`FPSCore_TestScene` **não é uma fase de jogo**. É um playground neutro,
propositalmente sem tema (nada de "cabana assombrada" ou qualquer arte de um
jogo específico), cujo único propósito é expor o Player a diferentes
condições de piso e obstáculo.

### GameObjects a criar

| GameObject | Componentes | Propósito no teste |
|---|---|---|
| `Ground` | `Plane` ou `Cube` achatado + `Mesh Collider`/`Box Collider` (não-trigger) | Piso plano de base — teste de movimento livre. |
| `Walls` (4 objetos ou 1 com colliders compostos) | `Cube` + `Box Collider` | Delimitam a área e testam colisão lateral (o Player não deve atravessar). |
| `Ramp` | `Cube` rotacionado (ex: 20°, 35°, 50° — crie 2 ou 3 com ângulos diferentes) + `Box Collider` | Testa a projeção de movimento na normal do chão (`PlayerMotor`, seção 7) em superfícies inclinadas. |
| `Steps` | 3–5 `Cube`s empilhados em degraus baixos (altura menor que `Step Offset`, seção 6) | Testa se o `CharacterController` sobe degraus pequenos sem travar nem pular. |
| `SmallObstacle` (2–3 objetos) | `Cube` pequeno + `Box Collider` | Testa colisão contra objetos baixos no caminho. |
| `Platform` (1–2, alturas diferentes) | `Cube` elevado + `Box Collider` | Testa queda (gravidade) e pulo até uma borda. |
| `HighLedge` | Uma plataforma alta o bastante pra **não** ser alcançável só andando | Testa se o pulo tem altura limitada corretamente (não deve ser possível subir nela só pulando, a menos que a `JumpHeight` do config permita). |

Nenhum desses objetos precisa de material bonito — cor sólida via `Material`
padrão do Unity já é suficiente pra enxergar as bordas.

### Configuração de colisores

- Todos os colisores de ambiente ficam como **não-trigger** (`Is Trigger`
  desmarcado) — o `CharacterController` precisa colidir fisicamente com eles.
- Não é necessário criar layers customizadas para o playground: o
  `_groundMask` do `PlayerMotor` (seção 7/8) tem default "Everything", então
  qualquer collider no cenário já conta como piso válido sem configuração
  extra. Só crie uma layer dedicada de "chão" se um teste específico exigir
  excluir algo (ex: um `SmallObstacle` que não deveria contar como piso para
  a projeção de rampa).

### Como testar rampas

Ande em direção à rampa em ângulos diferentes (de frente, na diagonal) e
observe: o personagem deve manter velocidade constante subindo/descendo, sem
"flutuar" no ar no topo nem "grudar" bruscamente. Se a rampa for mais íngreme
que o esperado, ajuste `Slope Limit` do `CharacterController` (seção 6) — não
o código do `PlayerMotor`.

### Como testar escadas

Ande contra os `Steps` sem pular. O `CharacterController` deve subir cada
degrau automaticamente enquanto a altura de cada um for menor que o `Step
Offset` configurado. Se travar num degrau, ou o degrau é mais alto que o
permitido (ajuste o cenário) ou o `Step Offset` está baixo demais para o caso
de uso (ajuste a config, não o script).

### Testar diferentes tipos de superfície no futuro

O `PlayerMotor` atual não distingue tipo de superfície (grama, metal, água) —
isso é intencionalmente um item do [Roadmap](#19-roadmap-futuro)
("Surface-based footsteps"). Quando essa feature existir, o playground ganha
blocos de `PhysicMaterial`/tag diferentes lado a lado para testar a troca de
som por superfície sem precisar de arte nenhuma.

---

## 5. Montagem do Player

### Hierarquia

```
Player
│
├── CameraPivot
│   │
│   └── Main Camera
│
└── (nenhum outro objeto obrigatório — extensões futuras entram aqui)
```

- **`Player`** é o objeto raiz: tem o `CharacterController` e é ele que
  fisicamente ocupa espaço e se desloca pelo cenário. Gira no eixo **Y**
  (esquerda/direita) — olhar para os lados gira o corpo inteiro.
- **`CameraPivot`** é um `GameObject` vazio, filho do `Player`, posicionado na
  altura dos olhos (ex: `Y = 1.6` para um personagem de ~1.8m). Ele não tem
  componentes de script — sua única função é ser o eixo de rotação **X**
  (olhar para cima/baixo) isolado do corpo. Se a câmera girasse no eixo X
  diretamente dentro do `Player`, o corpo inclinaria junto ao olhar para
  cima/baixo — errado para um controller em primeira pessoa. Separar em dois
  objetos resolve isso sem nenhuma lógica extra: o script só decide qual
  transform recebe cada eixo.
- **`Main Camera`** é filha do `CameraPivot`, na posição local `(0, 0, 0)`.

### Componentes no `Player`

| Componente | Obrigatório? | Configuração no Inspector |
|---|---|---|
| `Character Controller` (Unity) | Sim | Ver seção 6. |
| `Player Input Reader` (script) | Sim | Nenhum campo — plug-and-play. |
| `Player Motor` (script) | Sim | `_config` → asset de `PlayerMovementConfig` (seção 9). `_input` → o próprio `Player Input Reader`. `_groundMask` → deixe "Everything" a menos que o jogo precise excluir alguma layer. |
| `First Person Look` (script) | Sim | `_config` → mesmo asset. `_input` → `Player Input Reader`. `_playerBody` → transform do próprio `Player`. `_cameraPivot` → transform do `CameraPivot`. |
| `Audio Source` (Unity) | Só se usar footsteps | — |
| `Footstep Audio Player` (script) | Opcional | `_motor` → `Player Motor`. `_footstepClips`/`_jumpClip`/`_landClip` → clipes de áudio. |

Ordem de adição sugerida (evita ficar arrastando referência de componente que
ainda não existe): `Character Controller` → `Player Input Reader` →
`Player Motor` → `First Person Look` → (opcionais depois).

### `CameraPivot`

Sem componentes de script. Existe só como referência de transform para
`FirstPersonLook` aplicar a rotação vertical (`localRotation` em X) sem afetar
o corpo do `Player`, que gira no eixo Y separadamente. Não precisa de
collider nem de nada renderizável.

### Componentes na `Main Camera`

| Componente | Obrigatório? | Configuração no Inspector |
|---|---|---|
| `Camera` (Unity) | Sim | Padrão do Unity. |
| `Audio Listener` (Unity) | Sim | Já vem por padrão na Main Camera. |
| `Camera Head Bob` (script) | Opcional | `_config` → asset. `_motor` → `Player Motor` (está no `Player`, objeto pai). `_cameraTransform` → a própria `Main Camera`. |
| `Run Fov Kick` (script) | Opcional | `_config` → asset. `_motor` → `Player Motor`. `_camera` → o componente `Camera` desta própria câmera. |

Se a cena ainda tiver algum `FirstPersonController` do Standard Assets em
algum objeto (não deveria, num projeto-fonte limpo), remova — dois sistemas
de movimento no mesmo objeto vão brigar pelo `CharacterController`.

---

## 6. Configuração do CharacterController

Valores iniciais recomendados (ponto de partida confiável, não universal —
cada jogo ajusta ao tamanho do seu personagem e ao "feel" desejado):

| Campo | Valor inicial | O que controla |
|---|---|---|
| `Height` | `1.8` | Altura da cápsula de colisão. Deve refletir a altura aproximada do personagem. |
| `Radius` | `0.3` | Raio da cápsula. Afeta o quanto o personagem "arranha" em quinas e o quão perto ele chega de paredes. |
| `Center` | `(0, 0.9, 0)` | Centro da cápsula relativo ao pivô do objeto — metade da `Height`, para a cápsula ficar apoiada no chão em vez de flutuar/afundar. |
| `Step Offset` | `0.3` | Altura máxima de degrau que o personagem sobe automaticamente sem pular. Relacionado diretamente ao teste de `Steps` da seção 4. |
| `Slope Limit` | `45` | Ângulo máximo de rampa que o `CharacterController` considera "chão andável" (acima disso, ele trata como parede e o personagem escorrega/para). Ajuste junto com o teste de `Ramp`. |
| `Skin Width` | `0.08` (≈ `Radius / 4`) | Margem interna de tolerância que evita o personagem "tremer" (jitter) ao encostar em superfícies. Regra prática da própria Unity: manter entre 5% e 10% do `Radius`. |

Esses seis valores interagem entre si e com `PlayerMovementConfig` (seção 9)
— por exemplo, `StepOffset` só faz sentido testado junto dos degraus do
playground, e `SlopeLimit` só faz sentido testado nas rampas. Ajuste um de
cada vez e retorne ao [checklist de testes](#13-checklist-de-testes) após cada
mudança.

---

## 7. Scripts do FPSCore

### `PlayerInputReader.cs`

- **Responsabilidade:** ser o único script do pacote que conhece a existência
  do `UnityEngine.InputSystem`. Monta as `InputAction`s (Move, Look, Run,
  Jump) inteiramente em código no `Awake`, sem `.inputactions` asset.
- **O que ele não faz:** não decide o que fazer com o input (não move, não
  gira câmera) — só lê e expõe.
- **Localização:** `Runtime/Scripts/Input/`.
- **GameObject:** `Player`.
- **Dependências:** nenhuma de outro script do pacote.
- **Campos do Inspector:** nenhum.
- **Comunicação:** expõe `Vector2 MoveInput`, `Vector2 LookInput`,
  `bool RunHeld` (propriedades de leitura) e `event Action JumpPressed`.
  `PlayerMotor` e `FirstPersonLook` leem essas propriedades e se inscrevem no
  evento — nunca o inverso.

### `PlayerMotor.cs`

- **Responsabilidade:** mover o `CharacterController` — input relativo à
  câmera, projeção de movimento na normal do chão (`SphereCast` com
  `_groundMask`), pulo por altura desejada (fórmula física a partir de
  `JumpHeight`/`Gravity`, não um número mágico), transições chão/ar.
- **O que ele não faz:** não toca áudio, não gira câmera, não lê Esc/mouse
  diretamente, não sabe o que é diálogo ou cutscene — só expõe
  `SetMovementLocked(bool)` para quem quiser travar movimento de fora.
- **Localização:** `Runtime/Scripts/Movement/`.
- **GameObject:** `Player`.
- **Dependências:** `[RequireComponent(CharacterController)]`,
  referências serializadas a `PlayerMovementConfig` e `PlayerInputReader`.
- **Campos do Inspector:** `_config`, `_input`, `_groundMask` (LayerMask,
  default "Everything").
- **Comunicação:** expõe `event Action OnFootstep, OnJump, OnLand`,
  `event Action<bool> OnRunStateChanged`, `bool IsGrounded`,
  `float CurrentSpeed`, `bool IsRunning`, e o método
  `void SetMovementLocked(bool)`. `FootstepAudioPlayer`, `CameraHeadBob` e
  `RunFovKick` são os únicos consumidores hoje — qualquer sistema de jogo
  futuro (diálogo, IA que precisa saber a posição do jogador) se conecta do
  mesmo jeito, de fora, sem o Motor precisar saber que existem.

### `FirstPersonLook.cs`

- **Responsabilidade:** girar o corpo (`_playerBody`, eixo Y) e o
  `_cameraPivot` (eixo X, com clamp), e gerenciar lock/unlock de cursor
  (Esc solta, clique esquerdo trava).
- **O que ele não faz:** não move a posição do personagem, não lê o mapa de
  ações de gameplay para o toggle de cursor (lê `Keyboard`/`Mouse` diretamente
  — decisão justificada na seção 12).
- **Localização:** `Runtime/Scripts/Camera/`.
- **GameObject:** `Player` (mesmo objeto do Motor — ele referencia o
  `CameraPivot` por transform serializado, não precisa estar nele).
- **Dependências:** `PlayerMovementConfig`, `PlayerInputReader`, transforms de
  `_playerBody` e `_cameraPivot`.
- **Campos do Inspector:** `_config`, `_input`, `_playerBody`,
  `_cameraPivot`, `_deltaScale` (ajuste fino de sensibilidade — ver seção 9).
- **Comunicação:** expõe `void SetLookLocked(bool)`, simétrico ao
  `SetMovementLocked` do Motor.

### `PlayerMovementConfig.cs`

Ver seção 9, dedicada inteiramente a ele.

### `FootstepAudioPlayer.cs` (opcional)

- **Responsabilidade:** tocar `AudioClip`s ao escutar `OnFootstep`, `OnJump`,
  `OnLand` do `PlayerMotor`, sem repetir o mesmo clipe de passo duas vezes
  seguidas.
- **O que ele não faz:** não decide quando um passo "acontece" — isso é
  responsabilidade do Motor.
- **Localização:** `Runtime/Scripts/Audio/`.
- **GameObject:** `Player`.
- **Dependências:** `[RequireComponent(AudioSource)]`, referência a
  `PlayerMotor`.
- **Campos do Inspector:** `_motor`, `_footstepClips[]`, `_jumpClip`,
  `_landClip`.
- **Comunicação:** só escuta eventos do Motor — nunca é chamado por ninguém
  diretamente. É o exemplo de referência de "sistema opcional desacoplado"
  para qualquer add-on futuro.

### `CameraHeadBob.cs` (opcional)

- **Responsabilidade:** balançar ciclicamente `_cameraTransform` com base em
  `_motor.CurrentSpeed`/`IsGrounded`.
- **Localização:** `Runtime/Scripts/Effects/`.
- **GameObject:** `Main Camera`.
- **Dependências:** `PlayerMovementConfig`, `PlayerMotor` (leitura apenas).
- **Campos do Inspector:** `_config`, `_motor`, `_cameraTransform`.
- **Comunicação:** só lê propriedades públicas do Motor a cada frame — não
  precisa de evento porque não reage a uma transição pontual, e sim a um
  estado contínuo (velocidade).

### `RunFovKick.cs` (opcional)

- **Responsabilidade:** interpolar o `fieldOfView` da câmera ao escutar
  `OnRunStateChanged` do `PlayerMotor`.
- **Localização:** `Runtime/Scripts/Effects/`.
- **GameObject:** `Main Camera`.
- **Dependências:** `PlayerMovementConfig`, `PlayerMotor`, `Camera`.
- **Campos do Inspector:** `_config`, `_motor`, `_camera`.
- **Comunicação:** se inscreve no evento `OnRunStateChanged` — reage a
  transição, por isso usa evento em vez de polling (diferente do HeadBob).

---

## 8. Revisão de arquitetura e código

Revisão feita com o novo objetivo (projeto-fonte independente, reuso em
múltiplos jogos) em mente — não assumindo que a versão anterior estava
perfeita só por já existir.

### O que já estava certo e foi mantido

- **Separação em 7 arquivos de responsabilidade única** — continua
  apropriada, nenhum script faz mais do que uma coisa.
- **Comunicação só por método público/evento** — verificado em todos os 7
  scripts: nenhum acessa campo privado de outro, nenhum faz
  `GetComponent<T>()` buscando um tipo específico de jogo.
- **`PlayerMovementConfig` como `ScriptableObject` sem lógica** — confirmado,
  só campos públicos com `[Header]`.
- **Decisão de Input Actions em código** (em vez de `.inputactions` asset) —
  reavaliada em detalhe na seção 12 e **mantida**, com justificativa.
- **Namespace único `FPSCore`** em todos os scripts — consistente, sem
  conflito esperado com namespaces de jogo.

### O que foi identificado como problema e corrigido nesta revisão

1. **`PlayerMotor` usava `LayerMask` fixa (`~0`, todas as layers) no
   `SphereCast` de detecção de chão**, hardcoded no código. Isso era
   documentado como "ajuste conhecido" no README antigo (exigia editar o
   script na mão para restringir). Para um pacote reutilizável, qualquer
   ajuste que um jogo-consumidor precise fazer deve estar no Inspector, nunca
   exigir editar o script do pacote. **Corrigido:** novo campo serializado
   `_groundMask` (default "Everything", mesmo comportamento de antes até que
   alguém o troque). Ver `PlayerMotor.cs` atualizado e seção 6/7.
2. **Scripts estavam todos soltos em uma única pasta `Runtime/Scripts/`**,
   sem subpastas por responsabilidade — dificultava navegação à medida que o
   pacote crescesse (Roadmap, seção 19). **Corrigido:** reorganizados em
   `Scripts/Input/`, `Scripts/Movement/`, `Scripts/Camera/`,
   `Scripts/Audio/`, `Scripts/Effects/`, e `PlayerMovementConfig.cs` movido
   para `Runtime/Config/` (dados, deliberadamente fora de `Scripts/`, para
   reforçar visualmente "isso é dado, não comportamento"). Nenhuma mudança de
   `namespace` foi necessária — já era `FPSCore` em todos.
3. **`.asmdef` nomeado só `FPSCore`**, igual ao namespace — ambíguo quando o
   `Editor/` e `Tests/` (hoje reservados, vazios) ganharem seus próprios
   `.asmdef`s. **Corrigido:** renomeado para `FPSCore.Runtime.asmdef`
   (`"name": "FPSCore.Runtime"`), seguindo a convenção que a seção 15 já usa
   para os futuros `FPSCore.Editor` e `FPSCore.Tests`.

### O que foi avaliado e mantido sem mudança

- **`FirstPersonLook` lendo `Keyboard.current`/`Mouse.current` direto** (fora
  do `PlayerInputReader`) para o toggle de cursor. Poderia parecer uma
  inconsistência ("só um script deveria conhecer o Input System"), mas é uma
  exceção deliberada e documentada: lock/unlock de cursor é comportamento de
  UI pontual (Esc/clique), não uma ação de gameplay que precise fazer parte
  de um mapa de ações remapeável. Manter assim evita complicar o
  `PlayerInputReader` com uma quinta ação que não se encaixa no padrão das
  outras quatro (contínuas/analógicas).
- **`_deltaScale` como campo manual em `FirstPersonLook`** em vez de calculado
  automaticamente. Como o Input System novo não tem um equivalente direto ao
  `GetAxis("Mouse X")` antigo (delta vem em pixels/frame), não existe um valor
  "correto" universal — deixar exposto e ajustável no Inspector, por jogo, é
  mais honesto do que fingir um valor calculado que ainda precisaria de
  calibração manual de qualquer forma.
- **Fórmula de pulo por altura desejada** (`√(altura × -2 × gravidade)`) em
  vez de uma velocidade de pulo fixa — mantido porque é exatamente o que
  permite ajustar `Gravity` no config sem precisar recalibrar `JumpHeight`
  junto.

Nenhuma refatoração maior de arquitetura foi necessária — a base já foi
desenhada, desde o início desta conversa, pensando em reuso. As correções
acima são de "polimento de reutilização" (tirar hardcode, organizar pastas,
nomear `.asmdef` de forma extensível), não mudanças de comportamento.

---

## 9. PlayerMovementConfig

Um único `ScriptableObject`, sem lógica, concentra **todos** os números de
gameplay do pacote — a separação entre "código" (scripts) e "valores de
gameplay" (este asset) é o que permite o mesmo `PlayerMotor.cs` produzir um
personagem lento e pesado num jogo de terror e um personagem ágil noutro,
sem tocar em uma linha de C#.

### Como criar o asset

No painel Project, dentro da cena de testes (ou em qualquer pasta do
projeto-consumidor): botão direito → `Create > FPSCore > Player Movement
Config`. Nomeie de forma que fique claro a qual contexto pertence (ex:
`PMC_Default` no projeto-fonte; `PMC_Fotografo` num jogo específico).

### Onde ele fica

- No projeto-fonte (FPSCore Development): um asset de referência dentro de
  `Assets/_Sandbox/` (ou uma subpasta `Assets/Scenes/Config/`), usado só pela
  `FPSCore_TestScene`. Esse asset **não** faz parte do pacote — é dado de
  teste, não parte do produto.
- Em cada jogo que importar o pacote: o jogo cria o(s) seu(s) próprio(s)
  asset(s) de config, em qualquer pasta de `Assets/` daquele projeto — o
  pacote nunca inclui um config "de produção" pronto, só o
  `.cs` que define o formato.

### Como atribuir ao Player

Arraste o asset criado para o campo `_config` de **todos** os scripts que o
usam: `PlayerMotor`, `FirstPersonLook`, `CameraHeadBob` (se presente),
`RunFovKick` (se presente). Os quatro podem compartilhar o mesmo asset — é o
esperado, já que representam facetas do mesmo personagem.

### Valores que ele controla

| Grupo | Campos |
|---|---|
| Velocidade | `WalkSpeed`, `RunSpeed` |
| Pulo e gravidade | `JumpHeight`, `Gravity`, `StickToGroundForce` |
| Câmera / mouse | `MouseSensitivity`, `MinVerticalAngle`, `MaxVerticalAngle` |
| Som de passo | `WalkStepInterval`, `RunStepInterval` |
| Head bob (opcional) | `BobFrequency`, `BobAmplitude`, `BobSmoothSpeed` |
| FOV kick (opcional) | `RunFov`, `FovKickDuration` |

Essa organização já reflete a separação pedida: tudo que é "número de
gameplay" está aqui; tudo que é "layer/referência técnica" (como o novo
`_groundMask`) fica no campo do próprio script, porque é uma configuração de
integração com a cena, não uma escolha de design de gameplay.

---

## 10. Sistemas opcionais

Princípio central, válido para os três sistemas abaixo:

```
PlayerMotor funciona sozinho.
Sistemas adicionais apenas complementam o comportamento.
```

Isso é garantido estruturalmente: nenhum dos três é referenciado pelo
`PlayerMotor` ou `FirstPersonLook` — a dependência vai sempre do opcional para
o core, nunca o contrário. Remover qualquer um dos três componentes do
`Player`/`Main Camera` não quebra a compilação nem o movimento.

### FootstepAudioPlayer

- **Adicionar:** `Add Component > Footstep Audio Player` no `Player` (exige
  também um `Audio Source` no mesmo objeto). Arrastar `_motor` e os clipes.
- **Remover:** apagar o componente do `Player`. Nada mais precisa mudar.
- **Se o jogo não precisa de áudio de passo:** simplesmente não adicione o
  componente. O `PlayerMotor` continua disparando `OnFootstep` internamente
  (é barato — só um `event Action?.Invoke()` sem listener), mas nada escuta,
  então nada acontece.

### CameraHeadBob

- **Adicionar:** `Add Component > Camera Head Bob` na `Main Camera`. Arrastar
  `_config`, `_motor` (o `PlayerMotor` do objeto pai `Player`) e
  `_cameraTransform` (a própria Main Camera).
- **Configurar:** ajustar `BobFrequency`/`BobAmplitude`/`BobSmoothSpeed` no
  `PlayerMovementConfig` — não há campos exclusivos no script além das
  referências.
- **Desativar:** desmarcar o componente (`enabled = false`) ou removê-lo.
  Como ele só lê `CurrentSpeed`/`IsGrounded` do Motor a cada `Update`, isso
  não deixa nenhum estado pendente ao ser desativado.
- **Garantia de que não é obrigatório:** o componente não é referenciado por
  nenhum `[RequireComponent]` em outro script, e `PlayerMotor` não sabe que
  ele existe.

### RunFovKick

- **Adicionar:** `Add Component > Run Fov Kick` na `Main Camera`. Arrastar
  `_config`, `_motor`, `_camera` (o componente `Camera` da própria Main
  Camera).
- **Configurar:** `RunFov`/`FovKickDuration` no config.
- **Desativar:** remover ou desabilitar o componente. Ele se desinscreve do
  evento `OnRunStateChanged` em `OnDisable`, então não há vazamento de
  referência ao desativar em runtime.

---

## 11. Fluxo de comunicação entre os scripts

```
Input físico (teclado/mouse)
        │
        ▼
PlayerInputReader
        │
        ├─────────────► FirstPersonLook ──► rotação Player (Y) / CameraPivot (X)
        │                                    + cursor lock/unlock (lido direto,
        │                                      fora do mapa de ações)
        │
        ▼
   PlayerMotor
        │
        ├── Movimento (relativo à câmera, projetado no chão)
        ├── Corrida
        └── Pulo (fórmula por altura desejada)
        │
        ├──────────────► FootstepAudioPlayer   (escuta OnFootstep/OnJump/OnLand)
        │
        ├──────────────► CameraHeadBob         (lê CurrentSpeed/IsGrounded)
        │
        └──────────────► RunFovKick            (escuta OnRunStateChanged)
```

### Quem conhece quem

- `PlayerInputReader` não conhece ninguém — só expõe dados.
- `PlayerMotor` e `FirstPersonLook` conhecem `PlayerInputReader` e
  `PlayerMovementConfig` — são os dois "consumidores de input" do pacote.
- Os três sistemas opcionais conhecem `PlayerMotor` (leitura/evento) — nunca
  o contrário.
- **Nenhum script conhece um sistema de jogo** (diálogo, IA, UI). A via de
  mão única sempre é: sistema de jogo → se inscreve/chama método público do
  FPSCore.

### Quem deve evitar dependências

`PlayerMotor` e `PlayerInputReader` são o núcleo do núcleo — devem permanecer
sem nenhuma referência para fora do próprio pacote, para sempre. Se algum dia
uma feature exigir que `PlayerMotor` "saiba" de algo de jogo, é sinal de que
essa feature não pertence ao Motor — deve virar um sistema externo que lê o
Motor, como os três opcionais já fazem.

### Eventos vs. métodos públicos — quando usar qual

- **Evento:** para transições pontuais (aconteceu um passo, começou a
  correr, pousou) — o padrão de `OnFootstep`, `OnJump`, `OnLand`,
  `OnRunStateChanged`.
- **Propriedade de leitura:** para estado contínuo que outro sistema precisa
  consultar a cada frame (`IsGrounded`, `CurrentSpeed`) — usado por
  `CameraHeadBob`, que precisa de um valor a cada `Update`, não de uma
  notificação de mudança.
- **Método público:** para um sistema externo **comandar** o core (não só
  observá-lo) — `SetMovementLocked`, `SetLookLocked`. É a única via de
  "sistema de jogo → FPSCore" que existe hoje, e deve continuar sendo o
  padrão para qualquer controle futuro (ex: um `SetSpeedMultiplier(float)`
  no lugar de stamina, se isso vier a existir).

Os três mecanismos já cobrem tudo que os sistemas opcionais atuais precisam —
não há necessidade de introduzir interfaces (`IPlayerMotor` etc.) enquanto o
número de consumidores externos for baixo; isso fica registrado como
possibilidade futura caso o número de jogos/sistemas consumidores cresça o
bastante para justificar o desacoplamento adicional.

---

## 12. Sistema de Input

`PlayerInputReader` é o único ponto de contato com `UnityEngine.InputSystem`.
Duas abordagens foram avaliadas para a origem das `InputAction`s:

### Opção A — Input Actions criadas em código (atual, mantida)

**Vantagens**
- Zero asset extra para distribuir com o pacote — instalar e ativar o Input
  System já é suficiente (seção 2).
- Sem risco de conflito de nome com um `.inputactions` que o
  jogo-consumidor já tenha (dois assets de Input Actions no mesmo projeto
  podem confundir o Editor).
- Nada para "esquecer de configurar" — não depende de alguém lembrar de
  marcar "Generate C# Class" ou de importar o asset certo.

**Desvantagens**
- Rebind de teclas em runtime (tela de opções "redefinir controles") exige
  código adicional para localizar e substituir bindings named por convenção,
  em vez de usar a UI de rebind pronta que o Input System oferece para
  assets.
- Menos visual/descobrível no Editor — para inspecionar os bindings é preciso
  ler o `Awake()` do script, não abrir uma janela.

### Opção B — arquivo `.inputactions` incluído no pacote

**Vantagens**
- Editável visualmente (Interactions, Processors, Composite bindings pela
  UI).
- Compatível nativamente com a UI de rebind do próprio Input System
  (`InputActionRebindingExtensions`).

**Desvantagens**
- Vira um asset a mais para versionar, distribuir e manter dentro do pacote
  — motivo de conflito potencial se o jogo consumidor também tiver seu
  próprio esquema de Input Actions (para outros sistemas, como UI de menu).
- Exige o passo manual de "Generate C# Class" ficar marcado e consistente
  entre atualizações do pacote.

### Decisão mantida: Opção A

Para o estágio atual do FPSCore (core simples, sem tela de opções nem rebind
de teclas), a Opção A continua sendo a mais alinhada com a filosofia de
"plug-and-play sem configuração externa" da seção 1. Rebind de tecla é uma
feature de UI de jogo, não do movimento em si — se vier a ser necessária,
entra como um sistema **externo** que lê os bindings do `PlayerInputReader`
(possivelmente migrando **só essa parte** para Opção B, isoladamente, sem
afetar Motor/Look), e fica registrada no [Roadmap](#19-roadmap-futuro).

---

## 13. Checklist de testes

Rode este checklist na `FPSCore_TestScene` (seção 4) a cada mudança relevante
antes de considerar um incremento pronto para virar uma nova versão do
pacote (seção 18).

**Movimento**
- [ ] Player anda nas 4 direções relativo à câmera, não em eixo global.
- [ ] Player não atravessa `Walls`.
- [ ] Player respeita a inclinação das `Ramp`s sem flutuar ou travar.
- [ ] Player sobe os `Steps` sem travar (dentro do `Step Offset` configurado).

**Corrida**
- [ ] Segurar Shift aumenta a velocidade (`RunSpeed` vs. `WalkSpeed`).
- [ ] Soltar Shift volta à velocidade de andar sem sacão.

**Pulo**
- [ ] Pulo atinge aproximadamente a `JumpHeight` configurada.
- [ ] Apertar pulo um pouco antes de tocar o chão ainda funciona (fila de
      pulo, ver `PlayerMotor.OnJumpPressed`).
- [ ] Gravidade acumula corretamente no ar (queda acelera, não é linear).
- [ ] Não é possível alcançar o `HighLedge` só pulando.

**Câmera**
- [ ] Mouse horizontal gira o `Player` (corpo inteiro).
- [ ] Mouse vertical gira só o `CameraPivot`.
- [ ] Rotação vertical respeita `MinVerticalAngle`/`MaxVerticalAngle`.

**Cursor**
- [ ] Cursor trava ao iniciar a cena.
- [ ] Esc libera o cursor.
- [ ] Clique esquerdo trava de novo.

**Sistemas opcionais**
- [ ] `FootstepAudioPlayer`: som toca só andando no chão, sem repetir o
      mesmo clipe duas vezes seguidas.
- [ ] `CameraHeadBob`: balanço aparece ao andar/correr, some ao parar/pular.
- [ ] `RunFovKick`: FOV aumenta suavemente ao correr, volta ao soltar.
- [ ] Remover os três componentes opcionais não quebra movimento nem câmera.

**Travas externas**
- [ ] `motor.SetMovementLocked(true)` para o personagem sem erro, sem "sacão"
      residual de velocidade.
- [ ] `look.SetLookLocked(true)` trava só a câmera, corpo continua se
      movendo se `SetMovementLocked` não estiver ativo junto.

Se algo falhar, descreva o comportamento exato (ex: "personagem atravessa
parede na Ramp de 50°", "pulo não dispara na fila quando pressionado no ar")
para investigar isso especificamente antes de seguir.

---

## 14. Formato do pacote

### Opção A — UPM Package (`Packages/com.mateuskogl.fpscore`)

**Vantagens**
- Aparece no Package Manager como pacote de verdade — versão, dependências
  (`com.unity.inputsystem`) e samples geridos pela própria Unity.
- Suporta múltiplas formas de referência do lado do jogo-consumidor: pasta
  local (`file:`), git (`https://...git`), ou embutido — sem mudar nenhum
  script, só a entrada em `manifest.json`.
- `.asmdef` próprio → compilação isolada, mais rápida, e escopo de
  referência claro (o pacote só vê o que declarar em `references`).
- Assembly separada facilita, no futuro, escrever testes de Unit/PlayMode
  contra o pacote isoladamente (`Tests/`).

**Desvantagens**
- Setup inicial um pouco mais burocrático que simplesmente copiar arquivos
  (precisa de `package.json`, `.asmdef`, respeitar convenção de pastas).

### Opção B — `.unitypackage`

**Vantagens**
- Import por duplo-clique, familiar para quem já usou o Standard Assets
  (aliás, essa é literalmente a origem do FPSCore).

**Desvantagens**
- Não tem versionamento real — importar um `.unitypackage` novo por cima do
  antigo não remove arquivos deletados na versão nova, só sobrescreve/soma.
- Sem gerenciamento de dependência: o Input System teria que ser instalado
  manualmente em cada projeto, sem o pacote poder declarar isso.
- Não se integra ao Package Manager — nenhuma forma de "Update" com um
  clique, nenhuma distinção entre pacote e assets soltos do projeto.
- Pior fit para o fluxo "editar no FPSCore Development → jogo recebe
  atualização" que é justamente o objetivo deste projeto.

### Recomendação

**Opção A (UPM Package).** Para reuso nos seus próprios jogos — o caso de
uso declarado — o ganho de versionamento real, gestão de dependência
automática do Input System, e a flexibilidade de referência (local hoje, git
amanhã, sem tocar em script) supera a burocracia inicial, que já está feita:
a estrutura em `Packages/com.mateuskogl.fpscore/` (seção 3) já segue esse
formato desde o primeiro commit do projeto-fonte.

---

## 15. Estrutura final do pacote

```
com.mateuskogl.fpscore/
│
├── package.json
├── README.md
├── CHANGELOG.md
│
├── Runtime/
│   ├── FPSCore.Runtime.asmdef
│   ├── Config/
│   │   └── PlayerMovementConfig.cs
│   └── Scripts/
│       ├── Input/
│       │   └── PlayerInputReader.cs
│       ├── Movement/
│       │   └── PlayerMotor.cs
│       ├── Camera/
│       │   └── FirstPersonLook.cs
│       ├── Audio/
│       │   └── FootstepAudioPlayer.cs
│       └── Effects/
│           ├── CameraHeadBob.cs
│           └── RunFovKick.cs
│
├── Editor/                     ← reservado (FPSCore.Editor.asmdef quando usado)
│
├── Tests/                      ← reservado (FPSCore.Tests.asmdef quando usado)
│
├── Samples~/
│   └── DemoScene/               ← cena mínima equivalente à FPSCore_TestScene,
│                                    exportável via botão "Samples" do Package Manager
│
└── Documentation~/              ← documentação de referência do pacote (não este
                                     guia de desenvolvimento, que é do projeto-fonte)
```

`package.json` atual (`unity` deve refletir a versão mínima definida na
seção 2):

```json
{
  "name": "com.mateuskogl.fpscore",
  "version": "1.0.0",
  "displayName": "FPSCore - First Person Movement",
  "description": "Movimentação e câmera em primeira pessoa, extraídas e refinadas do Standard Assets, para reuso entre projetos.",
  "unity": "2022.3",
  "dependencies": {
    "com.unity.inputsystem": "1.0.0"
  },
  "keywords": ["fps", "first-person", "controller", "camera"],
  "author": {
    "name": "Mateus Kogl"
  }
}
```

`Samples~` e `Documentation~` levam til no nome propositalmente — é a
convenção do Package Manager para "pasta que existe dentro do pacote mas não
é importada automaticamente para o projeto consumidor". `Samples~` só aparece
como opção de import manual ("Samples" na aba do pacote); `Documentation~`
nunca é copiada, fica só como referência dentro da própria pasta do pacote.

---

## 16. Projeto-fonte vs. pacote final

| | Projeto de desenvolvimento (`FPSCore Development`) | Pacote final (`com.mateuskogl.fpscore`) |
|---|---|---|
| Contém | Cena de testes, playground (`_Sandbox`), asset de config de teste, este guia | Só o conteúdo de `Packages/com.mateuskogl.fpscore/` |
| Protótipos/testes | Sim — é o propósito do projeto | Não |
| Materiais/assets temporários | Sim, em `Assets/_Sandbox/` | Não |
| Versionado junto com o pacote? | Não precisa — ver abaixo | — |

### Como lidar com a separação na prática

Como o pacote já vive fisicamente isolado em `Packages/com.mateuskogl.fpscore/`
desde o início (seção 3), a separação não é um passo manual de "empacotar" —
é uma fronteira de pastas que já existe o tempo todo. O que muda é **o que
você versiona/distribui de cada lado**:

- Se você versionar `FPSCore Development` inteiro num único repositório git,
  a pasta `Packages/com.mateuskogl.fpscore/` dentro dele **é** o pacote — um
  jogo pode referenciá-la diretamente por caminho relativo (`file:`, seção
  17) sem precisar de repositório separado.
- Se/quando quiser distribuir o pacote de forma totalmente independente do
  projeto-fonte (outro repositório git, outra máquina, outro colaborador),
  basta que **só** o conteúdo de `Packages/com.mateuskogl.fpscore/` vá para
  esse novo lugar — nunca `Assets/`, `ProjectSettings/` ou qualquer coisa
  fora daquela pasta.

Não é necessário automatizar isso agora (script de export, CI) — o volume
atual (um projeto, um consumidor inicial) não justifica a complexidade. Se
o número de jogos consumidores crescer, isso entra como candidato ao
[Roadmap](#19-roadmap-futuro) de tooling (não de gameplay).

---

## 17. Instalar o FPSCore em um jogo novo

### Pré-requisito, em qualquer método: Input System instalado e ativado no jogo-consumidor (mesmos passos da seção 2).

### Método 1 — UPM local (`file:`), recomendado enquanto só você mantém o pacote

No `Packages/manifest.json` do jogo:

```json
{
  "dependencies": {
    "com.mateuskogl.fpscore": "file:../../FPSCore Development/Packages/com.mateuskogl.fpscore",
    "com.unity.inputsystem": "1.0.0"
  }
}
```

Ajuste o caminho relativo conforme onde `FPSCore Development` estiver no
disco em relação ao projeto do jogo. Editar os scripts na pasta de origem
reflete no jogo imediatamente (mesmo recarregamento de um pacote embutido),
sem precisar de git nem publicar nada.

Alternativa equivalente pela UI: `Window > Package Manager > + > Add package
from disk...` → selecione o `package.json` dentro de
`FPSCore Development/Packages/com.mateuskogl.fpscore/`.

### Método 2 — Git (quando o pacote for compartilhado além desta máquina)

```json
{
  "dependencies": {
    "com.mateuskogl.fpscore": "https://github.com/seu-usuario/fpscore.git",
    "com.unity.inputsystem": "1.0.0"
  }
}
```

Requer que o conteúdo de `Packages/com.mateuskogl.fpscore/` (com
`package.json` na raiz) esteja em um repositório git próprio — ver seção 16.

### Depois de instalado: montar o Player ou importar o Sample

**Fluxo A — importar o Sample**
1. `Window > Package Manager` → selecione FPSCore na lista → aba "Samples" →
   `Import` no `DemoScene`.
2. Abra a cena importada, já com `Player`/`CameraPivot`/`Main Camera`
   montados como referência — adapte à cena real do jogo, ou copie a
   hierarquia `Player` para dentro da cena do jogo.

**Fluxo B — montar manualmente**
Siga a seção 5 (Montagem do Player) e a seção 6 (CharacterController) deste
guia do zero na cena do jogo, criando um `PlayerMovementConfig` próprio do
jogo (seção 9) com os valores ajustados ao personagem daquele jogo.

Ambos os fluxos terminam no mesmo lugar — o Sample só acelera montar a
hierarquia; a configuração fina de gameplay (o asset de config) é sempre
específica de cada jogo.

---

## 18. Atualizar o pacote no futuro

Fluxo de manutenção, do desenvolvimento até a atualização em um jogo real:

```
1. Alterar o projeto FPSCore Development.
2. Testar na FPSCore_TestScene (checklist da seção 13).
3. Atualizar a versão em package.json e registrar em CHANGELOG.md.
4. (Se usando Método 2 - Git) Commitar e dar push no repositório do pacote.
5. No jogo consumidor: Package Manager → Update (git) ou apenas reabrir o
   projeto (file: local já reflete a mudança sem passo manual).
6. Rodar o checklist da seção 13 novamente, agora dentro do jogo real.
7. Corrigir problemas encontrados (voltar ao passo 1) antes de considerar a
   versão estável.
```

### Versionamento (SemVer: `MAJOR.MINOR.PATCH`)

| Tipo de mudança | Exemplo | Incrementa |
|---|---|---|
| Correção de bug, sem mudar comportamento esperado nem assinatura pública | Corrigir jitter no `SphereCast`, ajustar fórmula de pulo que estava com sinal errado | **Patch** (`1.0.0` → `1.0.1`) |
| Nova funcionalidade opcional, compatível com o que já existe | Adicionar `CrouchController.cs` novo, adicionar um evento novo em `PlayerMotor` | **Minor** (`1.0.1` → `1.1.0`) |
| Mudança que quebra compatibilidade | Renomear/remover um campo público, mudar assinatura de `SetMovementLocked`, remover um script | **Major** (`1.1.0` → `2.0.0`) |

Antes de `1.0.0`, uso normal de `0.x.y` é aceitável enquanto a API pública
ainda está se estabilizando (ex: `0.1.0` primeira versão funcional completa,
`0.2.0` ao adicionar `_groundMask`, etc.) — como o pacote já tem as 7
responsabilidades centrais estáveis e testadas, `1.0.0` é um ponto de partida
razoável a partir desta reorganização.

---

## 19. Roadmap futuro

O core permanece deliberadamente pequeno agora. Os itens abaixo são
candidatos a **scripts novos e opcionais**, que leem o core por fora — nunca
mudanças dentro de `PlayerMotor`/`FirstPersonLook`/`PlayerInputReader` — a
menos que, na hora de implementar, fique claro que o core precisa expor uma
propriedade/evento novo para o novo sistema se conectar (o padrão já usado
pelos três opcionais atuais).

- Crouch (agachar)
- Prone (deitar)
- Stamina (limite de corrida)
- Footsteps por tipo de superfície (`PhysicMaterial`/tag)
- Tratamento avançado de rampa/escorregão além do `Slope Limit` padrão
- Efeitos de aterrissagem (câmera, partícula, som de impacto por altura de queda)
- Camera shake (sistema genérico, não só para corrida)
- Sistema de interação (olhar + tecla → callback)
- Input mobile (toque/joystick virtual)
- Suporte a controle (gamepad)
- Rebind de teclas em runtime (ver nota da seção 12)

Nenhum desses deve ser iniciado sem necessidade concreta de um jogo real —
o objetivo atual do projeto permanece:

```
FIRST PERSON MOVEMENT CORE
simples, estável e reutilizável.
```

---

## 20. Checklist-mestre

Use isto como índice de "sei fazer isso a partir deste guia":

- [ ] Criar o projeto `FPSCore Development` do zero (seção 2)
- [ ] Organizar `Assets/` e `Packages/` (seção 3)
- [ ] Criar a `FPSCore_TestScene` com o playground (seção 4)
- [ ] Montar o `Player`/`CameraPivot`/`Main Camera` (seção 5)
- [ ] Configurar o `CharacterController` (seção 6)
- [ ] Saber onde cada script mora e em qual GameObject vai (seção 7)
- [ ] Configurar todas as referências de Inspector de cada script (seções 5 e 7)
- [ ] Entender o fluxo de comunicação entre scripts (seção 11)
- [ ] Rodar o checklist de testes (seção 13)
- [ ] Gerar/versionar o pacote (`Packages/com.mateuskogl.fpscore`, seções 14–15)
- [ ] Importar o pacote em um jogo novo (seção 17)
- [ ] Atualizar o pacote no futuro e saber quando subir patch/minor/major (seção 18)
