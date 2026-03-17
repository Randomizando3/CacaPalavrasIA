# CacaPalavrasIA

Aplicativo em .NET MAUI para gerar e jogar caca-palavras com apoio de IA, com foco em palavras tematicas e fallback local quando necessario.

## Destaques do projeto

- Geracao automatica do caca-palavras
- Tabuleiro interativo com validacao de palavras encontradas
- Lista de gabarito e acompanhamento do progresso
- Estrutura multiplataforma do .NET MAUI

## Estrutura principal

- `Services`: geracao das palavras e logica do puzzle
- `Models`: modelos do tabuleiro e das palavras
- `Views`: componentes visuais do jogo
- `Platforms`: configuracoes especificas por sistema operacional

## Como executar

1. Abra `CacaPalavrasIA/CacaPalavrasIA.sln` no Visual Studio 2022.
2. Garanta que o workload do .NET MAUI esteja instalado.
3. Restaure as dependencias e escolha a plataforma desejada para rodar.

## Observacao

Se o projeto depender de um servico externo de IA, mantenha qualquer chave de acesso apenas em configuracoes locais.