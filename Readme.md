# Обзор

## Инструкция по запуску приложения

### Конфигурация

#### База данных PostgreSQL

- **POSTGRES_USER**: Имя пользователя для подключения к PostgreSQL.
- **POSTGRES_PASSWORD**: Пароль для пользователя PostgreSQL.
- **POSTGRES_DB**: Название базы данных.

#### Среда выполнения ASP.NET Core

- **ASPNETCORE_ENVIRONMENT**: Среда выполнения приложения (например, Development, Production).

#### Строка подключения

- **ConnectionStrings__DefaultConnection**: Строка подключения к базе данных PostgreSQL.

#### Настройки электронной почты

- **EmailSettings__From**: Адрес электронной почты отправителя.
- **EmailSettings__SmtpServer**: Адрес SMTP сервера для отправки писем.
- **EmailSettings__Port**: Порт SMTP сервера.
- **EmailSettings__Username**: Имя пользователя для аутентификации на SMTP сервере.
- **EmailSettings__Password**: Пароль для SMTP сервера.
- **EmailSettings__EnableSendConfirmationEmail**: Включение отправки подтверждающих писем (`true` или `false`).

#### Аккаунт суперадминистратора сайта

- **RootUser__Email**: Электронная почта суперадминистратора.
- **RootUser__Password**: Пароль суперадминистратора.

### Запуск с использованием Docker Compose

1. **Установите Docker и Docker Compose.**

2. **Склонируйте репозиторий:**

   ```bash
   git clone https://github.com/KirillTotkov/DelphicGames.git
   cd DelphicGames
   ```

3. **Настройте файл .env:**
    - Скопируйте .env.example в .env и заполните необходимые переменные.
   ```bash
   cp .env.example .env
   ```
4. **Запустите контейнеры:**
   ```bash
   docker-compose up --build
   ```

### Запуск без Docker

1. **Установите .NET SDK и PostgreSQL.**

2. **Склонируйте репозиторий:**

   ```bash
   git clone https://github.com/KirillTotkov/DelphicGames.git
   cd DelphicGames
   ```

3. **Настройте файл appsettings.Development.json или appsettings.json:**

4. **Создайте базу данных:**

    - Запустите PostgreSQL и создайте базу данных `delphic_games`.
    - Используйте следующие команды:

   ```bash
   psql -U postgres
   CREATE DATABASE delphic_games;
   ```

5. **Выполните миграции:**

   ```bash
   dotnet ef database update
   ```

6. **Запустите приложение:**
   ```bash
   dotnet run
   ```
