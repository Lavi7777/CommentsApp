\# Comments App



SPA-приложение «Комментарии»: пользователи оставляют комментарии, отвечают на них (каскадное отображение любой вложенности), прикрепляют изображения и текстовые файлы. Тестовое задание, уровень Junior+.



Демо: будет добавлено после развёртывания



\## Возможности



\- Форма комментария: User Name (латиница и цифры), E-mail, Home page (необязательно), CAPTCHA (изображение), Text, вложение.

\- Ответы на любой комментарий без ограничения вложенности; в ответе над текстом показывается цитата родительского комментария.

\- Заглавные комментарии сортируются по User Name, E-mail и дате добавления (в обе стороны). По умолчанию новые сверху (LIFO).

\- Пагинация: 25 заглавных комментариев на страницу.

\- Разрешённые HTML-теги: `<a href="" title="">`, `<code>`, `<i>`, `<strong>`. Остальные теги запрещены, теги должны быть закрыты и правильно вложены (проверка на клиенте и на сервере).

\- Панель кнопок \[i] \[strong] \[code] \[a] и предпросмотр сообщения без перезагрузки страницы.

\- Изображения JPG, GIF, PNG: если больше 320×240, пропорционально уменьшаются (в фоновой очереди). Просмотр в окне lightbox. Текстовые файлы TXT до 100 КБ.

\- Новые комментарии появляются у всех посетителей сразу, без обновления страницы (WebSocket).

\- Для идентификации клиента сохраняются имя, e-mail, сайт, IP-адрес и User-Agent.



\## Стек



| Слой | Технологии |

|---|---|

| Backend | .NET 11, ASP.NET Core, Entity Framework Core, SignalR |

| База данных | PostgreSQL 17 (pgAdmin для просмотра) |

| Frontend | Angular, Angular Material, Tailwind CSS |

| Инфраструктура | Docker, Docker Compose, nginx |

| Тесты | xUnit (бэкенд) |



\## Уровень Junior+



| Требование | Реализация |

|---|---|

| Queue | Очередь на `Channel<T>` и фоновая служба: уменьшение загруженных изображений после ответа клиенту |

| Cache | `IMemoryCache`: кэш первых страниц списка комментариев, сбрасывается при новом комментарии |

| Events | События предметной области (`CommentCreatedEvent`) и обработчики: сброс кэша, рассылка по WebSocket, постановка задачи в очередь |

| WebSocket | SignalR, хаб `/hubs/comments` |



\## Быстрый запуск (Docker)



Нужны Docker и Docker Compose.



```bash

git clone https://github.com/Lavi7777/comments-app.git

cd comments-app

cp .env.example .env

```



Откройте `.env` и задайте пароли (без символов `;` и `=`):



```

POSTGRES\_PASSWORD=ваш\_пароль

PGADMIN\_EMAIL=ваша\_почта@example.com

PGADMIN\_PASSWORD=ваш\_пароль

WEB\_PORT=80

```



Запуск:



```bash

docker compose up -d --build

```



Приложение откроется на `http://localhost` (или на порту из `WEB\_PORT`). Миграции базы данных применяются автоматически при старте API.



Доступ к pgAdmin только с самого сервера: `http://127.0.0.1:5050` (вход: `PGADMIN\_EMAIL` и `PGADMIN\_PASSWORD`). Подключение к базе: host `postgres`, port `5432`, база `commentsdb`, пользователь `comments\_user`.



Остановка: `docker compose down` (данные сохраняются в томах). Полная очистка: `docker compose down -v`.



\## Разработка без Docker для приложения



База поднимается в Docker, API и Angular запускаются локально.



```bash

docker compose up -d postgres pgadmin

cd Back/CommentsApp.Api

dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=commentsdb;Username=comments\_user;Password=ваш\_пароль"

dotnet run --launch-profile http

```



В другом окне:



```bash

cd Front

npm ci

npm start

```



Приложение: `http://localhost:4200` (запросы к `/api`, `/uploads` и `/hubs` проксируются на API порта 5247).



\## Тесты



```bash

cd Back

dotnet test

```



Покрыто: проверка HTML-тегов и защита от XSS, CAPTCHA, хранение и уменьшение изображений, кэш, события, контроллер комментариев (создание, пагинация, сортировка, вложенность).



\## API



| Метод | Адрес | Назначение |

|---|---|---|

| GET | `/api/comments?page=1\&sortBy=date\&desc=true` | Список заглавных комментариев с ответами (`sortBy`: `username`, `email`, `date`) |

| POST | `/api/comments` | Создать комментарий (`multipart/form-data`) |

| GET | `/api/captcha` | Получить CAPTCHA (идентификатор и изображение) |

| POST | `/api/preview` | Предпросмотр: очищенный HTML или ошибка проверки тегов |

| GET | `/api/comments/count` | Количество комментариев |

| WS | `/hubs/comments` | SignalR, событие `commentCreated` |



\## Безопасность



\- XSS: текст разбирается по белому списку тегов, всё остальное экранируется; ссылки допускаются только `http` и `https`.

\- SQL-инъекции: все запросы выполняются через Entity Framework Core с параметрами.

\- Файлы: тип определяется по содержимому, а не по расширению; на диске файлы хранятся под случайными именами; отдаются с заголовком `X-Content-Type-Options: nosniff`.

\- CAPTCHA одноразовая и живёт 5 минут.

\- Порты базы данных и pgAdmin привязаны к `127.0.0.1` и недоступны снаружи.



\## Структура



```

Back/

&#x20; CommentsApp.Domain/          сущности, события, интерфейсы (Abstractions)

&#x20; CommentsApp.Infrastructure/  EF Core, миграции, реализации сервисов, обработчики событий

&#x20; CommentsApp.Api/             контроллеры, SignalR-хаб, фоновая служба

&#x20; CommentsApp.Tests/           xUnit-тесты

Front/                         Angular-приложение и nginx.conf

docs/                          схема БД: db-schema.sql и db-schema.mwb (MySQL Workbench)

docker-compose.yml

```



\## Схема базы данных



Файлы `docs/db-schema.sql` и `docs/db-schema.mwb` открываются в MySQL Workbench. Рабочая база PostgreSQL; схема для Workbench написана в синтаксисе MySQL и повторяет таблицы `users`, `comments`, `attachments` и связи между ними (`comments.ParentId` ссылается на `comments.Id`, что даёт вложенные ответы).



\## Ограничения



\- GIF сохраняется как PNG (анимация не сохраняется).

\- Кэш хранится в памяти одного экземпляра API; при нескольких экземплярах понадобится распределённый кэш (например, Redis).

