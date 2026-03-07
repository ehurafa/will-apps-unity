# Will Apps Unity

Migração do aplicativo **will-apps-pwa** (React/TypeScript) para uma aplicação nativa Android usando **Unity + C#**.

## Sobre o Projeto

O objetivo é portar os jogos e funcionalidades do app PWA para Unity, ganhando melhor performance, suporte a jogos mais complexos e maior compatibilidade com dispositivos Android.

**Repositório:** `git@github.com:ehurafa/will-apps-unity.git`

---

## Regras de Negócio

### Público-Alvo
- **Usuário principal:** Criança de 12 anos.
- O app deve ser **simples, intuitivo e bonito** (com cara de jogo).
- Roda em um **único aparelho Android**.

### Conceito do App
O app é um hub de entretenimento pessoal com duas grandes seções:

1. **Assistir** 🎬 — Streaming de vídeos (Animes e Filmes).
2. **Jogar** 🎮 — Coleção de mini-jogos casuais.

### Fluxo de Navegação

```
MainMenu (Home)
├── Assistir (Watch)
│   ├── Tab: Animes
│   └── Tab: Filmes
│   └── VideoPlayer (ao selecionar um vídeo)
└── Jogar (Play)
    ├── Flappy Bird
    ├── Jogo da Velha (vs IA)
    ├── Flappy Ninja (tema Naruto)
    ├── Sinuca (futuro)
    └── UNO (futuro)
```

### Seção "Assistir" (Watch)
- Organizada em categorias via **tabs**: `Animes` e `Filmes`.
- Os vídeos são listados em um grid com **thumbnail**, **título** e **duração**.
- Ao clicar em um vídeo, abre o player em tela cheia.
- **Origem dos vídeos:** Hospedados na Hostinger (hospedagem tradicional), consumidos via URL direta (MP4).
- Funcionalidade de **transmitir para TV** (cast) é desejável.

### Seção "Jogar" (Play)
- Lista de jogos em cards com ícone, nome e descrição.
- Jogos indisponíveis aparecem como **"Bloqueado"**.
- Todos os jogos são **single-player** (1 jogador contra a máquina).

### Regras dos Jogos

#### Flappy Bird
- O jogador controla um pássaro e deve desviar de canos.
- **Toque na tela** faz o pássaro pular.
- Canos surgem da direita com **gap aleatório** entre o cano superior e inferior.
- **Pontuação:** +1 ponto ao passar por cada par de canos.
- **Game Over:** Colidir com cano ou sair dos limites da tela (topo/fundo).
- **High Score:** Salvo localmente no dispositivo.

#### Jogo da Velha (Tic-Tac-Toe)
- Jogador é sempre **X**, IA é sempre **O**.
- Jogador começa primeiro.
- **IA com estratégia:** Tenta vencer → Bloqueia jogador → Centro → Cantos → Qualquer espaço.
- **Delay da IA:** 500ms antes de jogar (para parecer que "pensa").
- **Placar persistente** durante a sessão (Jogador vs IA).
- Botão de **resetar placar** e **jogar novamente**.
- Detecta empate quando todas as células estão preenchidas.

#### Flappy Ninja (tema Naruto) — Futuro
- Variação do Flappy Bird com tema do anime Naruto.
- Especificações desejadas:
  - Escolha de personagem: Naruto, Sasuke ou Sakura.
  - Animação ao pular.
  - Sons: fundo (`theme.mp3`), pulo (`jump.mp3`), colisão (`collision.mp3`).
  - Background com leve animação de scale.

#### Sinuca e UNO — Futuro
- Planejados mas ainda não iniciados.
- Serão single-player contra IA.

### Hospedagem de Assets
- Todos os assets e mídias que precisem de hospedagem serão hospedados na **Hostinger** (serviço de hospedagem tradicional).
- Upload via **FTP**.
- Consumo via URL direta no app.

### Design e Identidade Visual

#### Paleta de Cores (do app original)
| Cor                | Hex       | Uso                          |
|--------------------|-----------|------------------------------|
| Primary            | `#FF6B6B` | Vermelho vibrante            |
| Secondary          | `#4ECDC4` | Azul-turquesa                |
| Accent             | `#FFE66D` | Amarelo (destaque, bird)     |
| Success            | `#95E1D3` | Verde água (pipes)           |
| Background         | `#1A1A2E` | Azul escuro (fundo geral)    |
| Card               | `#16213E` | Azul card                    |
| Text               | `#FFFFFF` | Texto principal              |
| Text Secondary     | `#A8DADC` | Texto secundário             |

#### Gradientes
- **Primary:** `#FF6B6B` → `#FF8E53` (botão Assistir)
- **Secondary:** `#4ECDC4` → `#44A08D` (botão Jogar)
- **Accent:** `#FFE66D` → `#FFAB4C` (cards de jogos)

### Convenções de Código
- **Nomes de componentes, funções e variáveis:** Inglês.
- **Textos de UI (labels, mensagens):** Português (pt-BR).
- **Naming no Unity:** PascalCase para classes/scripts, camelCase para variáveis.
- **GameObjects no Editor:** Prefixo `Btn_` para botões.

---

## Stack

| Tecnologia       | Versão / Detalhe          |
|------------------|---------------------------|
| Unity            | 6 LTS (6000.0.x)         |
| Linguagem        | C#                        |
| Template         | Universal 2D (URP)        |
| Plataforma Alvo  | Android                   |
| IDE              | Visual Studio Community 2022 |

---

## Estrutura do Projeto

```
Assets/
├── Scenes/
│   ├── MainMenu.unity        ✅ Configurada
│   ├── VideoPlayer.unity      ✅ Configurada
│   └── FlappyBird.unity       🔧 Em progresso
├── Scripts/
│   ├── MainMenuController.cs  ✅ Completo
│   └── VideoPlayerController.cs ✅ Completo
├── Settings/
└── TextMesh Pro/
```

---

## Funcionalidades

### 1. Main Menu (Cena: `MainMenu`) ✅

Menu principal com navegação entre as telas do app.

- **Script:** `MainMenuController.cs`
- **GameObjects:**
  - `Canvas` com 3 botões (TextMeshPro):
    - `Btn_VideoPlayer` → Carrega cena `VideoPlayer`
    - `Btn_FlappyBird` → Carrega cena `FlappyBird`
    - `Btn_TicTacToe` → Carrega cena `TicTacToe`
  - `MainMenuController` (Empty Object com o script anexado)
- **Canvas Scaler:** Scale With Screen Size (1080x1920)

### 2. Video Player (Cena: `VideoPlayer`) ✅

Player de vídeo nativo do Unity com streaming via URL.

- **Script:** `VideoPlayerController.cs`
- **Configuração do Video Player:**
  - Source: URL
  - Render Mode: Camera Near Plane
  - URL padrão: Big Buck Bunny (MP4)
- **GameObjects:**
  - `Video Player` (componente nativo)
  - `Canvas` com botões:
    - `Btn_Back` → Volta para `MainMenu`
    - `Btn_PlayPause` → Toggle Play/Pause
  - `VideoController` (Empty Object com o script anexado)

### 3. Flappy Bird (Cena: `FlappyBird`) 🔧 Em Progresso

Jogo Flappy Bird com física 2D, baseado no original do PWA.

- **Concluído:**
  - Cena criada
  - Objeto `Bird` (Circle Sprite amarelo, Scale 0.5)
    - Componentes: `Rigidbody2D`, `CircleCollider2D`
- **Pendente:**
  - `BirdController.cs` (pulo ao tocar na tela, colisão)
  - `PipeSpawner.cs` (gerar canos com gap aleatório)
  - `PipeMove.cs` (mover canos para a esquerda)
  - `GameManager.cs` (pontuação, game over, restart)
  - Prefab dos canos (top/bottom + trigger de pontuação)
  - UI: Texto de pontuação, painel de Game Over

### 4. Jogo da Velha / Tic-Tac-Toe (Cena: `TicTacToe`) ❌ Não Iniciado

- **Pendente:**
  - Cena `TicTacToe`
  - `TicTacToeController.cs` (lógica do jogo, UI)

---

## Configurações Importantes

### Build Profiles (Android)
- Plataforma já configurada para **Android** via Build Profiles.
- Módulos instalados: Android Build Support, Android SDK & NDK Tools, OpenJDK.

### Canvas Scaler (UI Mobile)
Todas as cenas com UI devem usar:
- **UI Scale Mode:** Scale With Screen Size
- **Reference Resolution:** 1080 x 1920
- **Screen Match Mode:** Match Width Or Height (Match: 0.5)

### Scenes no Build
As seguintes cenas devem estar registradas no Build Profiles:
1. `MainMenu` (index 0 — cena inicial)
2. `VideoPlayer`
3. `FlappyBird`
4. `TicTacToe` *(quando criada)*

---

## Referência: App Original (PWA)

O app original está em `c:\PROJECTS\will-apps-pwa` e contém:

| Funcionalidade   | Arquivo Original                          |
|------------------|-------------------------------------------|
| Home / Menu      | `src/pages/Home.tsx`                      |
| Video Player     | `src/pages/VideoPlayer.tsx`               |
| Flappy Bird      | `src/games/FlappyBird.tsx`                |
| Jogo da Velha    | `src/games/JogoDaVelha.tsx`               |
| Cores do tema    | `src/constants/colors.ts`                 |

### Constantes do Flappy Bird (original)
```
GRAVITY = 0.5
JUMP_STRENGTH = -8
PIPE_WIDTH = 60
PIPE_GAP = 150
PIPE_SPEED = 2
BIRD_RADIUS = 15
CANVAS_SIZE = 400x600
BACKGROUND_COLOR = #87CEEB
BIRD_COLOR = #FFE66D
PIPE_COLOR = #95E1D3
```

---

## Como Rodar

1. Abra o **Unity Hub**.
2. Adicione o projeto `c:\PROJECTS\will-apps-unity`.
3. Abra com o Unity 6 LTS.
4. Na pasta `Assets/Scenes`, abra a cena `MainMenu`.
5. Pressione **Play** (▶) para testar no Editor.

### Build Android (APK)
1. **File > Build Profiles** (`Ctrl+Shift+B`).
2. Selecione **Android**.
3. Clique em **Build** e escolha onde salvar o APK.
4. Instale o APK no dispositivo Android.
