## Sandbox (ACDH Sandbox Framework) — guia dos scripts (sem código)

Este `README.md` é um **guia prático e detalhado** do que cada script em `Assets/SandboxFramework/Scripts` faz e como você usa isso **só pelo Inspector** (UnityEvents/Triggers), sem escrever código.

- **Objetivo do framework**: permitir prototipar sistemas emergentes combinando física + seleção/arrasto + solda (weld) + eventos (UnityEvents).
- **Mentalidade**: “scripts pequenos, componíveis” → você monta lógica como LEGO, conectando eventos no Inspector.

## Como ler este guia

- **O que faz**: comportamento em runtime.
- **Onde usar**: em qual GameObject costuma ficar.
- **Campos do Inspector**: o que cada campo controla.
- **Eventos/saídas**: quais UnityEvents você pode conectar.
- **Armadilhas comuns**: coisas que quebram o setup e como evitar.

## Estrutura do framework (mapa rápido)

- **Scripts de base (raiz de `Scripts/`)**
  - `Selectable.cs`
  - `Draggable.cs`
  - `Weldable.cs`
- **Triggers (gatilhos “sem código”)**: `Scripts/Triggers/`
  - `ConditionalTrigger.cs`, `RecipeTrigger.cs`, `RandomTrigger.cs`, `TimedEventTrigger.cs`
- **Controllers (máquinas/variáveis/spawn/switch)**: `Scripts/Controllers/`
  - `StateMachine.cs`, `Switch.cs`, `Destroyer.cs`, `Spawner.cs`, `Variable.cs`, `RandomValue.cs`
- **Interação do player**: `Scripts/Core/PlayerInteractionScripts/`
  - `SelectionHandler.cs`, `DragHandler.cs`, `Welder.cs`, `RaycastMouseTrigger.cs`, `PlayerCollisionTrigger.cs`
- **UI**: `Scripts/Core/PlayerInteractionScripts/UI/`
  - `DisplayItemInfo.cs`, `CrossHairPositioning.cs`
- **EventListeners (adaptadores para UnityEvents)**: `Scripts/EventListeners/`
  - `TriggerListener.cs`, `MouseListener.cs`, `CollisionListener.cs`, `DragListener.cs`, `WeldListener.cs`, `KeyListener.cs`, `KeyPressListener.cs` (via Utilities), `SeatListener.cs`, `SwitchListener.cs`, `VehicleSeatListener.cs`, `GameObjectListener.cs`, etc.
- **Itens/veículos**: `Scripts/Items/`
  - `Seat.cs`, `VehicleSeat.cs`, `MovingPlatform.cs`, `Wheel.cs`
- **Utils**: `Scripts/Core/Utils/`
  - `InputSystem.cs`, `PlayerInputActions.cs` (gerado), `Utils.cs`, `CustomFixedJoint.cs`, `KeepUpright.cs`, `PlayerComponentInstaller.cs`, `TeleportationBehaviour.cs`
- **Interfaces (contratos)**: `Scripts/Core/PlayerInteractionScripts/Utilities/`
  - `IActivatable.cs`, `IDragListener.cs`, `IWeldListener.cs`, `ISeatListener.cs`, `IVehicleListener.cs`, `IKeyPressListener.cs`, etc.

---

## Scripts de base (raiz)

### `Assets/SandboxFramework/Scripts/Selectable.cs`

- **O que faz**: marca um objeto como “selecionável” e oferece um texto (`ObjectDescription`) para UI/tooltip.
- **Onde usar**: em qualquer objeto que você quer que o jogador “aponte e selecione” (normalmente no mesmo GameObject do collider principal).
- **Campos do Inspector**
  - **Object Description**: string livre, usada por UI como `DisplayItemInfo`.
- **Armadilhas comuns**
  - `SelectionHandler` procura `Selectable` no **pai** do collider (`GetComponentInParent<Selectable>()`). Se seu collider estiver num filho, garanta que o `Selectable` esteja em algum ancestral.

### `Assets/SandboxFramework/Scripts/Draggable.cs`

- **O que faz**: permite que o objeto seja “arrastado” (grab/drag) pelo `DragHandler`. Também propaga eventos de grab/release para listeners conectados (ex.: `DragListener`) e lida com welds.
- **Onde usar**: em objetos que o player deve mover no mundo (copos, ingredientes, peças de máquina).
- **Campos do Inspector**
  - **shouldPropagateDragEvents**: se `true`, envia `OnGrab/OnRelease` para todos `IDragListener` na hierarquia/weld group (bom pra efeitos e sons).
  - **shouldIgnoreRigidbodySettingFromDragger**: se `true`, o `DragHandler` não vai forçar `isKinematic` durante drag/release.
  - **removeFromParentAtAwake**: se `true`, solta o objeto da hierarquia no `Awake` (útil se você instanciou como filho mas quer virar objeto independente).
- **Como funciona por baixo**
  - `StartDrag(...)`: marca como “sendo arrastado”, dispara eventos de grab, ajusta rigidbodies (se permitido).
  - `UpdateDrag(pos, rot)`: move o objeto e atualiza joints (`CustomFixedJoint.UpdateJoint`).
  - `EndDrag(...)`: dispara release, restaura rigidbody, aplica “arremesso” (throwVelocity).
- **Armadilhas comuns**
  - Para arrasto ficar estável, o root normalmente deve ter **Collider + Rigidbody**.
  - Se você estiver usando welds do tipo Hierarchy, o script tenta aplicar mudanças de rigidbody em toda a “peça conectada”.

### `Assets/SandboxFramework/Scripts/Weldable.cs`

- **O que faz**: define um objeto que pode ser “soldado/ligado” a outros objetos (`WeldTo`) e depois separado (`Unweld`). Pode operar em:
  - **HierarchyBased**: reparent (vira filho de outro transform).
  - **PhysicsBased**: cria `CustomFixedJoint` (joints físicos).
- **Onde usar**: em objetos que você quer “montar” (peças de máquina, blocos, veículos, estruturas).
- **Como interage com o resto**
  - `Welder` chama `Weldable.WeldTo(...)` quando você aperta o botão de weld.
  - `Draggable` e `Switch` usam weld connections pra propagar comportamento na estrutura.
- **Armadilhas comuns**
  - Se você misturar weld types no mesmo grupo pode dar warnings (“type mismatch”).
  - Auto-weld em `Start()` tenta soldar com um ancestral `Weldable` (se existir).

---

## Triggers (gatilhos)

### `Assets/SandboxFramework/Scripts/Triggers/ConditionalTrigger.cs`

- **O que faz**: avalia condições baseadas nos colliders atualmente dentro do trigger e dispara UnityEvents de **sucesso** ou **falha**.
- **Onde usar**: zonas de validação (ex.: “entrega correta”), sockets (ex.: “copo no lugar”), portas, áreas que habilitam/desabilitam coisas.
- **Campos do Inspector (principais)**
  - **conditions**: lista de condições. Cada condição tem:
    - `conditionType`: `ObjectName`, `Tag`, `Layer`, `State`, `MaterialName`, `AnimatorState`, `Any`
    - `value`: o valor comparado (nome, tag, layer name, estado do `StateMachine`, etc.)
    - `negate`: inverte o resultado (vira “NOT”)
  - **conditionLogic**: `And` (todas verdadeiras) ou `Or` (qualquer uma).
  - **autoTrigger**: reavalia automaticamente ao entrar/sair do trigger.
  - **TimeToEvaluate**: atraso antes de avaliar (útil pra “esperar encaixar”).
  - **evaluationOrder / ProceedOrderOnResult**: permite encadear vários `ConditionalTrigger` no mesmo GameObject como “if / else if / else” (ordens 0,1,2…).
  - **onConditionsMet / onConditionsFailed**: UnityEvents para conectar ações.
- **Dica de ouro**
  - A condição `State` lê `StateMachine.CurrentState` do **objeto que entrou** no trigger. Isso é perfeito pra “estado = visual” sem código.

### `Assets/SandboxFramework/Scripts/Triggers/RecipeTrigger.cs`

- **O que faz**: detecta “receitas” (conjunto de ingredientes) dentro de um trigger por **nome de GameObject** e, após `reactionTime`, dispara um evento da receita.
- **Campos do Inspector**
  - **recipes**: lista de receitas; cada receita tem:
    - `ingredients`: lista de strings com os nomes dos objetos (case-insensitive). Duplicatas contam (ex.: “Egg”, “Egg”).
    - `onRecipeMatched`: UnityEvent disparado quando bate.
  - **reactionTime**: tempo que os ingredientes precisam permanecer presentes.
  - **autoDestroy**
    - `true`: destrói os objetos usados no match (ótimo pra “consumir laranja”).
    - `false`: “trava” os colliders usados até saírem e entrarem de novo (evita repetir).
- **Armadilhas comuns**
  - O match é por `col.gameObject.name`. Se seu prefab chama “Orange(Clone)”, renomeie a instância ou ajuste o fluxo (ex.: Spawner define `instance.name = prefab.name`, ajudando).

### `Assets/SandboxFramework/Scripts/Triggers/RandomTrigger.cs`

- **O que faz**: escolhe aleatoriamente um `UnityEvent` de uma lista e invoca.
- **Onde usar**: escolher pedidos do cliente, variar efeitos, “eventos emergentes”.
- **Campos**
  - `events[]`: lista de UnityEvents.
  - `invokeOnStart`: se dispara um evento aleatório ao iniciar.
- **Uso típico**
  - Pedido do NPC: 3 eventos (Beer/Juice/Soda) → cada um liga um visual e habilita validadores.

### `Assets/SandboxFramework/Scripts/Triggers/TimedEventTrigger.cs` (classe `TimerTrigger`)

- **O que faz**: agenda eventos em tempos específicos, com modo `Once/Loop/PingPong`.
- **Campos**
  - `events`: lista de `(delay, UnityEvent)`
  - `mode`: Once / Loop / PingPong
  - `playbackSpeed`, `autoPlay`, `duration`, `startTime`
- **Onde usar**
  - Sequências (ex.: luz piscando, “cliente reagindo” após X segundos, reset do pedido).

---

## Controllers (controle/estado/variáveis/spawn)

### `Assets/SandboxFramework/Scripts/Controllers/StateMachine.cs`

- **O que faz**: máquina de estados simples por string. Cada estado tem `OnStart` e `OnStop` (UnityEvents).
- **Onde usar**
  - Estados visuais (copo: Empty/Beer/Juice/Soda).
  - Estados de máquina (ligado/desligado/erro).
- **Campos**
  - `startState`: nome do estado inicial (`"None"` desativa tudo).
  - `states[]`: lista de estados com `name`, `OnStart`, `OnStop`.
- **API (pra UnityEvent)**
  - `SetState(string stateName)`: troca estado e dispara eventos.
- **Armadilhas comuns**
  - Nomes são case-insensitive, mas têm que existir. Se não existir, loga warning.

### `Assets/SandboxFramework/Scripts/Controllers/Switch.cs`

- **O que faz**: um “switch” que chama `OnActivate/OnDeactivate` em componentes que implementam `IActivatable` (ex.: `SwitchListener`), dentro do mesmo root/weld group.
- **Campos**
  - `activationGroup` (Color): funciona como “ID de canal” (só ativa listeners com a mesma cor).
  - `shouldBroadcast`: se `true`, procura listeners na cena inteira (até fora do weld group).
- **Métodos (pra UnityEvent)**
  - `TurnOn()`, `TurnOff()`, `Toggle()`, `Reactivate()`.
- **Onde usar**
  - “Botões” físicos: ao pressionar, chama `Switch.Toggle()`; do outro lado, `SwitchListener` dispara eventos.

### `Assets/SandboxFramework/Scripts/EventListeners/SwitchListener.cs` (também é parte do sistema de Switch)

- **O que faz**: implementa `IActivatable` e expõe UnityEvents:
  - `onTurnOn`, `onTurnOff`
- **Campos**
  - `switchGroup` (Color): tem que bater com `Switch.activationGroup`.
- **Uso típico**
  - Coloque `Switch` no botão/lever e `SwitchListener` no “alvo” (ou no mesmo grupo soldado).

### `Assets/SandboxFramework/Scripts/Controllers/Destroyer.cs`

- **O que faz**: rastreia todos os objetos dentro de um trigger e destrói todos quando você chama `DestroyOverlappingItems()` (por UnityEvent).
- **Onde usar**
  - Lixeira, triturador, “máquina de suco” que consome itens, limpeza da cena.
- **Como usar**
  - Coloque `Destroyer` num GameObject com Collider `isTrigger = true`.
  - Conecte algum evento (botão/trigger) para chamar `DestroyOverlappingItems()`.
- **Armadilha**
  - Ele destrói **todos** os objetos que estiverem dentro quando você chamar (sem filtro de tag). Se precisa filtro, combine com `ConditionalTrigger` habilitando/desabilitando o `Destroyer`.

### `Assets/SandboxFramework/Scripts/Controllers/Spawner.cs`

- **O que faz**: instancia um prefab aleatório de `prefabs[]` em `spawnLocation`.
- **Campos**
  - `spawnLocation`: Transform; se vazio, usa o próprio GameObject.
  - `prefabs[]`: lista de prefabs.
  - `scale`: multiplicador de escala.
- **Detalhe importante**
  - Ele define `instance.name = prefab.name`, o que ajuda `RecipeTrigger` (evita “(Clone)” atrapalhar receita).

### `Assets/SandboxFramework/Scripts/Controllers/Variable.cs`

- **O que faz**: controla uma variável `float` com clamp e mudança automática por segundo, dispara `UnityEvent<float>` ao mudar e avalia condições (edge-trigger false→true).
- **Onde usar**
  - Barras (fome/sede/temperatura), timers simples, “progress”.
- **Campos**
  - `value`, `minValue`, `maxValue`
  - `changePerSecond`: altera continuamente
  - `smoothUpdate`: por frame (suave) vs em “degraus” por segundo
  - `onValueChange(float)`
  - `conditions[]`: cada condição tem comparação + evento + `stopIfTrue` (prioridade)
- **Extra**
  - Tenta sincronizar com `Animator` do pai usando o **nome do GameObject** como parâmetro float (se existir).

### `Assets/SandboxFramework/Scripts/Controllers/RandomValue.cs`

- **O que faz**: guarda um `int`, pode randomizar no start e/ou em intervalo, e avalia condições disparando eventos em true/false.
- **Uso típico**
  - Sorteio de pedido (embora `RandomTrigger` já resolva), estados discretos, fases.
- **Campos**
  - `value`, `minValue`, `maxValue`
  - `randomOnStart`, `autoRandom`, `intervalSeconds`
  - `onValueChange(int)`
  - `conditions[]`: chama `OnEvaluateToTrue` ou `OnEvaluateToFalse`
  - `scriptConditions[]` + `RunScriptCondition(int valueToTest)` para comparar com um valor externo

---

## Core / PlayerInteractionScripts (como o player interage)

### `Assets/SandboxFramework/Scripts/Core/PlayerInteractionScripts/SelectionHandler.cs`

- **O que faz**: “seleciona” o objeto sob o cursor via raycast, desde que exista `Selectable` na hierarquia. Para feedback, muda o `layer` para uma layer de destaque (por padrão `Selection`).
- **Campos**
  - `selectionLayerName`: nome da layer usada para highlight.
  - `raycastDistance`: alcance do raycast.
  - `currentSelection`: (read-only) seleção atual.
- **Detalhe importante**
  - Se o usuário segura `InputButton.ShowHierarchy`, ele seleciona o `transform.root` do objeto (útil para weld groups).
- **Armadilhas**
  - Você precisa ter a layer `Selection` criada no projeto, ou `LayerMask.NameToLayer` pode retornar -1 e o highlight fica inconsistente.

### `Assets/SandboxFramework/Scripts/Core/PlayerInteractionScripts/DragHandler.cs`

- **O que faz**: ao clicar/segurar, começa a arrastar o objeto selecionado (se ele tiver `Draggable`). Dá snap em grid e permite rotações por teclas.
- **Campos principais**
  - **Grid**
    - `useGrid`, `gridSize`, `gridCenter`
  - **Rotação**
    - `allowRotation` (None/YawOnly/All)
    - `rotationSnapDegrees` (snap por eixo, em graus)
  - **Rigidbody**
    - `rigidbodyStateChangeOnDrag` (ex.: SetKinematic)
    - `rigidbodyStateChangeOnRelease`
    - `throwMultiplier`, `maxThrowVelocity`
  - `toggleToDrag`: se `true`, clique alterna drag (não precisa segurar).
- **Fluxo**
  - Usa `SelectionHandler.currentSelection`.
  - Chama `Draggable.StartDrag(...)`, `UpdateDrag(...)`, `EndDrag(...)`.

### `Assets/SandboxFramework/Scripts/Core/PlayerInteractionScripts/Welder.cs`

- **O que faz**: permite “soldar” (`Weld`) e “dessoldar” (`Unweld`) objetos `Weldable` quando o player aperta botões de input.
- **Campos**
  - `WeldProximityThreshold`: margem para detectar sobreposição/encaixe.
  - `weldingType`: HierarchyBased ou PhysicsBased.
- **Como decide o weld**
  - Usa `Physics.ComputePenetration` para checar “penetração” e tenta achar `Weldable` sobreposto que não está no mesmo root.
- **Observação**
  - Há um comentário no código sugerindo que a condição de penetração pode estar invertida; na prática, se seu weld estiver “estranho”, ajuste o threshold e teste.

### `Assets/SandboxFramework/Scripts/Core/PlayerInteractionScripts/RaycastMouseTrigger.cs`

- **O que faz**: faz um raycast do centro da tela e, quando o player clica, chama `MouseListener.OnMouseDown/Up` no collider atingido.
- **Onde usar**
  - Para botões clicáveis sem UI (um cubo com collider vira “botão”).

### `Assets/SandboxFramework/Scripts/Core/PlayerInteractionScripts/PlayerCollisionTrigger.cs`

- **O que faz**: captura colisões do `CharacterController` do player (`OnControllerColliderHit`) e encaminha para `CollisionListener` nos objetos atingidos (enter/stay/exit simulados).
- **Onde usar**
  - Se você quer reações específicas quando o player encosta em algo, mas está usando `CharacterController`.

---

## UI

### `Assets/SandboxFramework/Scripts/Core/PlayerInteractionScripts/UI/DisplayItemInfo.cs`

- **O que faz**: mostra na UI (TextMeshPro) o nome e a descrição (`Selectable.ObjectDescription`) do objeto selecionado — mas só quando não está arrastando.
- **Campos**
  - `selectionHandler`, `dragHandler`
  - `itemName` (TMP), `itemDescription` (TMP)

### `Assets/SandboxFramework/Scripts/Core/PlayerInteractionScripts/UI/CrossHairPositioning.cs`

- **O que faz**: move um `RectTransform` (crosshair) para seguir a posição do mouse.
- **Campos**
  - Não expõe campos; pega `RectTransform` e `Canvas` automaticamente.

---

## EventListeners (ponte para UnityEvents)

### `Assets/SandboxFramework/Scripts/EventListeners/TriggerListener.cs`

- **O que faz**: expõe `OnTriggerEnter/Stay/Exit` como UnityEvents (sem filtro por padrão).
- **Uso**
  - Um trigger simples: quando algo entra, chama um UnityEvent (ex.: ligar luz, tocar som, chamar `Switch.Toggle()`).
- **Limitação**
  - O script não passa “qual objeto entrou” para o evento.

### `Assets/SandboxFramework/Scripts/EventListeners/MouseListener.cs`

- **O que faz**: expõe `onMouseDownEvent` e `onMouseUpEvent`.
- **Como é acionado**
  - Pelo `RaycastMouseTrigger` do player (não depende do OnMouseDown clássico do Unity UI).

### `Assets/SandboxFramework/Scripts/EventListeners/CollisionListener.cs`

- **O que faz**: expõe UnityEvents de colisão física (`OnCollisionEnter/Stay/Exit`) e colisão “do player” (`OnPlayerCollisionEnter/Stay/Exit`).
- **Como o “player collision” funciona**
  - `PlayerCollisionTrigger` chama esses métodos quando o CharacterController colide.

### `Assets/SandboxFramework/Scripts/EventListeners/DragListener.cs`

- **O que faz**: implementa `IDragListener` e expõe `onGrab` / `onRelease`.
- **Quem chama**
  - `Draggable` chama esses eventos quando começa/termina drag (e pode propagar pelo weld group).

### `Assets/SandboxFramework/Scripts/EventListeners/WeldListener.cs`

- **O que faz**: implementa `IWeldListener` e expõe 4 UnityEvents:
  - `onWeld`, `onUnweld`, `onAdded`, `onRemoved`
- **Quem chama**
  - `Weldable` notifica listeners quando conecta/desconecta.

### `Assets/SandboxFramework/Scripts/EventListeners/KeyListener.cs`

- **O que faz**: listener global de teclas (InputSystem do Unity, `Keyboard.current[...]`) e dispara UnityEvents em press/release.
- **Uso**
  - Bind rápido sem mexer no `InputSystem` do framework.

### `Assets/SandboxFramework/Scripts/EventListeners/GameObjectListener.cs`

- **O que faz**: “transforma” ciclos de vida do MonoBehaviour em UnityEvents (`onStart`, `onUpdate`, `onFixedUpdate`, `onLateUpdate`, `onDestroy`).
- **Uso**
  - Timers simples (com seu próprio Update), inicialização visual, logs.

### `Assets/SandboxFramework/Scripts/EventListeners/SeatListener.cs`

- **O que faz**: recebe eventos de `Seat` (sentar/levantar) e expõe `onSeat`/`onUnseat` como UnityEvents.
- **Como chega o evento**
  - `Seat` procura `ISeatListener` no weld group/hierarquia e chama `OnSeat/OnUnseat`.

### `Assets/SandboxFramework/Scripts/EventListeners/VehicleSeatListener.cs`

- **O que faz**: ouve eventos de um “veículo” (assento + steer + throttle) e dispara UnityEvents com valores processados.
- **Campos (alto nível)**
  - Pré-processamento (invert/abs/clamp/remap) para steer/throttle.
  - `onSteer(float)`, `onThrottle(float)`, `onSeat`, `onUnseat`.
  - Detecta posição da roda (FrontLeft/FrontRight/RearLeft/RearRight etc.) ao soldar.
- **Uso**
  - Conectar input do veículo a animações, força de motor, direção, etc., tudo via Inspector.

### `Assets/SandboxFramework/Scripts/EventListeners/SwitchListener.cs`

(documentado junto do `Switch.cs` acima — é o par do sistema.)

---

## Items (itens prontos)

### `Assets/SandboxFramework/Scripts/Items/Seat.cs`

- **O que faz**: permite o player “sentar” ao entrar num trigger (se o seat estiver “welded/ativo”) e sair ao pressionar `InputButton.Jump`.
- **Campos**
  - `seatPoint`: onde o player fica sentado (posição/rotação).
  - `playerTag`: tag do player (default “Player”).
  - `debounceDuration`: cooldown pra não sentar/levantar instantaneamente.
  - `onSeat`, `onUnseat`: UnityEvents.
  - `keysToCheck`: lista de teclas que, quando pressionadas, são encaminhadas para `IKeypressListener` conectados.
- **Detalhes importantes**
  - Desabilita `FirstPersonController` e `CharacterController` do player ao sentar.
  - Move/reparent para o `seatPoint`.
  - Usa reflection pra chamar `CameraRotation` do StarterAssets enquanto sentado.

### `Assets/SandboxFramework/Scripts/Items/VehicleSeat.cs`

- **O que faz**: estende `Seat` para enviar steer/throttle para `IVehicleListener` enquanto ocupado.
- **Uso**
  - Montar veículos com peças soldáveis e controlar via assento.

### `Assets/SandboxFramework/Scripts/Items/MovingPlatform.cs`

- **O que faz**: move `CharacterController`s que estão “em cima” de uma plataforma, compensando deslocamento/rotação.
- **Como**
  - Detecta personagens com `Physics.OverlapBox` acima da plataforma e aplica delta de posição/rotação.

### `Assets/SandboxFramework/Scripts/Items/Wheel.cs`

- **O que faz**: sincroniza o transform visual da roda com `WheelCollider` (posição/rotação física).

---

## Core / Utils

### `Assets/SandboxFramework/Scripts/Core/Utils/InputSystem.cs`

- **O que faz**: wrapper estático em cima do novo Input System (classe gerada `PlayerInputActions`).
- **Fornece**
  - `GetButtonDown(InputButton)`, `GetButton(InputButton)`
  - `GetPointerPosition()`, `GetPointerDown()`, `GetPointerUp()`, `GetPointerHeld()`
  - `GetAxis(InputAxis)` com smoothing e deadzone
- **Por que importa**
  - `DragHandler`, `SelectionHandler`, `Welder`, `RaycastMouseTrigger`, `VehicleSeat` usam isso.

### `Assets/SandboxFramework/Scripts/Core/Utils/PlayerInputActions.cs`

- **O que faz**: arquivo **auto-gerado** pelo Unity Input System (`.inputactions`).
- **Regra**
  - Não edite manualmente. Ele define as actions (Weld, Unweld, PointerPress, PointerPosition, etc.) e bindings.

### `Assets/SandboxFramework/Scripts/Core/Utils/Utils.cs`

- **O que faz**: utilitário para encontrar componentes em toda a hierarquia + conexões de weld (`FindAllInHierarchyAndConnections<T>`).
- **Quem usa**
  - `Draggable`, `Seat`, `VehicleSeat`, etc., para propagar eventos no weld group.

### `Assets/SandboxFramework/Scripts/Core/Utils/CustomFixedJoint.cs`

- **O que faz**: implementa um “junta/ligação” custom.
  - Se tiver `Rigidbody` no alvo, cria `FixedJoint` e vira `PhysicsBased`.
  - Senão, vira `HierarchyBased` e atualiza transform do alvo manualmente com offset.
- **Quem usa**
  - `Weldable` no modo PhysicsBased e `Draggable.UpdateDrag` chama `CustomFixedJoint.UpdateJoint(...)`.

### `Assets/SandboxFramework/Scripts/Core/Utils/KeepUpright.cs`

- **O que faz**: mantém o objeto “em pé” (reseta rotação) quando ele não tem parent.
- **Uso**
  - Objetos que você quer impedir de tombar (pode ser útil em protótipos blocky).

### `Assets/SandboxFramework/Scripts/Core/Utils/PlayerComponentInstaller.cs`

- **O que faz**: em runtime, encontra o player capsule (`CharacterController`) e adiciona scripts por nome (`Type.GetType`).
- **Uso**
  - Setup dinâmico em protótipos/demos (mas exige nome qualificado se tiver namespace).

### `Assets/SandboxFramework/Scripts/Core/Utils/TeleportationBehaviour.cs`

- **O que faz**: teleporta o `CharacterController` para um `Transform` alvo de forma segura (desliga controller, move, liga), e “warpa” Cinemachine para evitar glitch.
- **Uso**
  - Portais, respawn, “voltar ao balcão”.

---

## Interfaces (Utilities)

Estas interfaces são “contratos” usados para o framework achar componentes e chamar métodos sem acoplamento:

- `IActivatable`: usado por `Switch`/`SwitchListener`
- `IDragListener`: usado por `Draggable`/`DragListener`
- `IWeldListener`: usado por `Weldable`/`WeldListener`
- `ISeatListener`: usado por `Seat`/`SeatListener`
- `IVehicleListener`: usado por `VehicleSeat`/`VehicleSeatListener`
- `IKeyPressListener`/`IKeypressListener`: usados por `Seat` + `KeyPressListener`

---

## Debug

### `Assets/SandboxFramework/Scripts/Debug/DebugMonitor.cs`

- **O que faz**: cria um overlay de debug em runtime (UI) para logs e também exibir o estado de até 3 `StateMachine`s.
- **Campos**
  - `enableOnStart`, `widthPercent`, `heightPercent`, `anchorPosition`
  - `showTimestamp`
  - `stateMachine1/2/3`
- **Atalho**
  - `Shift + C` limpa o console do overlay.

---

## Padrões práticos de montagem (sem código) — exemplos rápidos

### Padrão: “estado visual” com `StateMachine`

- Crie `Cup` (root) com filhos `Visual_Empty`, `Visual_Beer`, etc.
- Coloque `StateMachine` no root.
- Em cada `State.OnStart`, conecte 4 chamadas `GameObject.SetActive(bool)` para garantir que só um visual fica ativo.

### Padrão: “validação em zona” com `ConditionalTrigger`

- Em `DeliveryZone` (Collider isTrigger):
  - `ConditionalTrigger.conditions`: `Tag = Cup` + `State = Beer`
  - `onConditionsMet`: feedback positivo
  - `onConditionsFailed`: feedback negativo

### Padrão: “consumir ingrediente” com `RecipeTrigger`

- Em `JuicerSlot`:
  - `RecipeTrigger.autoDestroy = true`
  - `ingredients = ["Orange"]`
  - `onRecipeMatched`: `Cup.StateMachine.SetState("Juice")`

---

## Referência externa

- Canal/playlist relacionado ao projeto: `https://www.youtube.com/channel/UC_0Xyq99ojTGRBDiInsopng`
