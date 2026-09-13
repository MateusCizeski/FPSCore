# FPSCore — Movimentação em Primeira Pessoa (pacote reutilizável)

Pacote extraído e refinado a partir do `FirstPersonController.cs` / `MouseLook.cs` /
`CurveControlledBob.cs` / `FOVKick.cs` do Standard Assets (Unity 2018.4). A lógica de
movimento (projeção no chão, pulo por altura desejada, stick-to-ground) e de câmera foi
reimplementada do zero em cima do **Input System novo**, dividida em scripts pequenos e
independentes, para reuso direto em qualquer jogo em primeira pessoa.

Não é mais um guia "escreva você mesmo" — é o pacote pronto. Os arquivos ficam em
`Runtime/Scripts/`, organizados por responsabilidade (`Input/`, `Movement/`, `Camera/`,
`Audio/`, `Effects/`), com `PlayerMovementConfig.cs` em `Runtime/Config/`.

> **Este README é a referência rápida de instalação/uso do pacote.** Para o processo
> completo de desenvolvimento — como o projeto-fonte `FPSCore Development` é organizado,
> como montar a cena de testes, revisão de arquitetura, checklist de testes, geração e
> atualização de versão — veja **[`FPSCore_Development_Guide.md`](./FPSCore_Development_Guide.md)**.

---

## 1. O que tem aqui

| Script                    | Responsabilidade                                                                                                                                 | Obrigatório? |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | ------------ |
| `PlayerInputReader.cs`    | Único script que conhece o Input System. Expõe `MoveInput`, `LookInput`, `RunHeld`, evento `JumpPressed`.                                        | Sim          |
| `PlayerMotor.cs`          | Move o `CharacterController`: input relativo à câmera, projeção na normal do chão, pulo por altura (`SetMovementLocked` como ponto de extensão). | Sim          |
| `FirstPersonLook.cs`      | Rotação de corpo (Y) e câmera (X), clamp vertical, lock de cursor (`SetLookLocked` como ponto de extensão).                                      | Sim          |
| `PlayerMovementConfig.cs` | `ScriptableObject` com todos os números (velocidade, pulo, sensibilidade, etc).                                                                  | Sim          |
| `FootstepAudioPlayer.cs`  | Escuta `OnFootstep`/`OnJump`/`OnLand` do Motor e toca áudio, sem repetir o mesmo clipe duas vezes seguidas.                                      | Opcional     |
| `CameraHeadBob.cs`        | Balanço de câmera ao andar (lê `CurrentSpeed`/`IsGrounded` do Motor).                                                                            | Opcional     |
| `RunFovKick.cs`           | Zoom de FOV ao correr (escuta `OnRunStateChanged` do Motor).                                                                                     | Opcional     |

Nenhum desses scripts referencia algo específico de um jogo (UI, diálogo, etc). Toda
comunicação externa acontece por método público ou evento — é assim que outros sistemas
(diálogo, cutscene, o que vier) se conectam sem o pacote precisar conhecê-los.

**Sobre a origem do código:** a lógica (projeção de movimento na normal do chão, fórmula
de pulo por altura, clamp de câmera, seleção de som de passo sem repetição) segue o
comportamento do Standard Assets original (licenciado sob MIT pela Unity Technologies),
mas o código foi reescrito do zero — arquitetura em 7 arquivos, Input System novo em vez
de `CrossPlatformInputManager`, e API baseada em eventos. Não é uma cópia literal.

---

## 2. Instalação num projeto novo

### 2.1 Pré-requisito: pacote Input System

`Window > Package Manager > Unity Registry` → instalar **Input System**.
`Edit > Project Settings > Player > Active Input Handling` → **"Input System Package (New)"**
(ou "Both", se o projeto ainda usa o sistema antigo em outro lugar).

> Diferente do guia original, **não é preciso criar um `.inputactions` asset** — o
> `PlayerInputReader` monta as ações (Move/Look/Run/Jump) em código. Isso deixa o pacote
> plug-and-play: só instalar o Input System já é suficiente.

### 2.2 Importar o pacote FPSCore num jogo

**Este pacote já é desenvolvido como pacote** — vive em
`Packages/com.mateuskogl.fpscore/` dentro do projeto-fonte independente
`FPSCore Development` (não dentro de nenhum jogo). Um jogo que queira usá-lo só precisa
referenciá-lo, nunca copiá-lo. Duas formas, na ordem recomendada:

**A) Dependência local (`file:`) — recomendado enquanto só você mantém o pacote**

No `Packages/manifest.json` do jogo:

```json
{
  "dependencies": {
    "com.mateuskogl.fpscore": "file:../../FPSCore Development/Packages/com.mateuskogl.fpscore",
    "com.unity.inputsystem": "1.0.0"
  }
}
```

Ajuste o caminho relativo à posição real de `FPSCore Development` no disco. Editar os
scripts no projeto-fonte reflete no jogo imediatamente — sem git, sem publicar nada.
Equivalente pela UI: `Window > Package Manager > + > Add package from disk...` →
selecione o `package.json` dentro de `FPSCore Development/Packages/com.mateuskogl.fpscore/`.

**B) Git — quando o pacote precisar ser compartilhado além desta máquina**

```json
{
  "dependencies": {
    "com.mateuskogl.fpscore": "https://github.com/seu-usuario/fpscore.git",
    "com.unity.inputsystem": "1.0.0"
  }
}
```

Requer criar um repositório git separado só com o conteúdo desta pasta
(`package.json` na raiz do repositório). Cada jogo referencia esse repositório no próprio
`manifest.json`; atualizações chegam pelo botão "Update" do Package Manager, ou fixando
uma tag/branch com `#nome-da-tag` no fim da URL.

**Nunca copie os scripts direto para dentro de `Assets/` de um jogo** — isso cria uma
cópia independente que para de receber atualizações do pacote original.

Ver **`FPSCore_Development_Guide.md`, seções 14–18** para o racional completo (por que
UPM em vez de `.unitypackage`, estrutura final do pacote, e o fluxo de versionamento ao
atualizar).

---

## 3. Setup na cena

### 3.1 Hierarquia final

Isso é o que você vai ter montado ao final dos passos abaixo:

```
Player                          (CharacterController, PlayerInputReader, PlayerMotor,
                                  FirstPersonLook, AudioSource, FootstepAudioPlayer)
└── CameraPivot                 (objeto vazio, sem script)
    └── Main Camera             (Camera, AudioListener, CameraHeadBob, RunFovKick)
```

- **`Player`** é o objeto raiz, com um `CharacterController` — é ele que fisicamente
  se move pelo cenário e gira no eixo Y (olhar pros lados).
- **`CameraPivot`** é um `GameObject` vazio (`Create Empty`), filho do `Player`, na
  altura dos "olhos" do personagem (ex: `Y = 1.6`). Ele só existe pra girar no eixo X
  (olhar pra cima/baixo) sem girar o corpo do `Player` junto.
- **`Main Camera`** fica filha do `CameraPivot` (posição local `0,0,0`), com o
  `AudioListener` que já vem por padrão nela.

### 3.2 Passo a passo

1. **Criar o config:** `Assets > Create > FPSCore > Player Movement Config` → ajustar
   valores de velocidade, pulo, sensibilidade etc no Inspector. Salve em qualquer pasta
   (ex: `Assets/Configs/`).
2. **Montar a hierarquia** exatamente como o diagrama acima: `Player` → `CameraPivot`
   → `Main Camera`.
3. **No `Player`**, adicionar (via `Add Component`) nesta ordem:
   - `Character Controller` (componente da Unity) — ajuste `Height`/`Radius`/`Center`
     pro tamanho do seu personagem.
   - `Player Input Reader` (script) — não tem campo pra preencher.
   - `Player Motor` (script) — arraste o **config** criado no passo 1 pro campo `_config`,
     e arraste o próprio `Player` (ou o componente `Player Input Reader`) pro campo `_input`.
   - `First Person Look` (script) — arraste o **config**, o `Player Input Reader`, o
     transform do próprio `Player` em `_playerBody`, e o transform do `CameraPivot` em
     `_cameraPivot`.
4. **No `Main Camera`**, confirme que ela tem o componente `Camera` (padrão) e um
   `Audio Listener` (padrão). Não precisa de script nenhum nela pra o núcleo funcionar.
5. **Áudio de passo (opcional):** no `Player`, adicionar `Audio Source` +
   `Footstep Audio Player` (script) → arraste o `Player Motor` no campo `_motor`, e os
   clipes de som (passo/pulo/aterrissagem) nos campos correspondentes.
6. **Head bob (opcional):** na `Main Camera`, adicionar `Camera Head Bob` (script) →
   arraste o config, o `Player Motor` (que está no `Player`, objeto pai), e o transform
   da própria `Main Camera` em `_cameraTransform`.
7. **FOV kick (opcional):** na `Main Camera`, adicionar `Run Fov Kick` (script) →
   arraste o config, o `Player Motor`, e a própria `Camera` (componente) em `_camera`.
   Teste sem os opcionais primeiro, adicione um de cada vez depois.

Se a cena ainda tiver o `FirstPersonController` original do Standard Assets em algum
objeto, **remova ou desative** — dois sistemas de movimento no mesmo objeto vão brigar.

---

## 4. Pontos de extensão (pra outros sistemas se conectarem)

- `motor.SetMovementLocked(true/false)` — trava movimento horizontal (diálogo, cutscene,
  qualquer restrição futura).
- `look.SetLookLocked(true/false)` — trava só a câmera, independente do corpo.
- `motor.OnFootstep`, `OnJump`, `OnLand`, `OnRunStateChanged` — eventos públicos pra
  qualquer sistema (áudio, partícula de poeira, medidor de ruído) se inscrever sem o
  Motor precisar conhecê-lo. Siga o padrão do `FootstepAudioPlayer.cs` como referência.

---

## 5. Ajustes conhecidos

- **Sensibilidade do mouse:** `<Mouse>/delta` do Input System vem em pixels por frame,
  numa escala diferente do `GetAxis("Mouse X")` antigo. O `FirstPersonLook` já aplica um
  fator de escala (`_deltaScale`, padrão `0.02`) antes de multiplicar pela sensibilidade
  do config — ajuste esse campo ou `MouseSensitivity` no olho até sentir bem.
- **`SphereCast` de projeção no chão:** controlado pelo campo `_groundMask` do
  `PlayerMotor` (Inspector), padrão "Everything". Se o jogo tiver layers que não devem
  contar como "chão" (ex: gatilhos, VFX), restrinja a `LayerMask` ali — não é mais
  necessário editar o script.

---

## 6. Checklist de teste

- [ ] Personagem anda nas 4 direções relativo à câmera, não em eixo global
- [ ] Pulo funciona mesmo apertando o botão um pouco antes de tocar o chão
- [ ] Personagem segue inclinação de rampa/terreno sem flutuar ou travar
- [ ] Sensibilidade do mouse está num nível confortável (ajustar `_deltaScale`/`MouseSensitivity`)
- [ ] Som de passo toca só andando no chão, sem repetir o mesmo clipe duas vezes seguidas
- [ ] `SetMovementLocked(true)` para o personagem sem quebrar nada
- [ ] Cursor trava/destrava corretamente com Esc e clique esquerdo
- [ ] Head bob e FOV kick (se ativados) não "brigam" com a câmera do `FirstPersonLook`

Se algo falhar, descreva o comportamento exato (ex: "personagem atravessa parede",
"pulo não funciona nunca") pra ajustarmos juntos.
