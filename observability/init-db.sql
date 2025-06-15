-- =================================================================
-- SCRIPT DE INICIALIZAÇÃO DO BANCO DE DADOS 'shop'
-- Este script cria as tabelas necessárias para a funcionalidade
-- de Pedidos (Orders), alinhadas com o modelo de domínio da aplicação.
-- =================================================================

-- Habilita a extensão para gerar UUIDs, se ainda não estiver habilitada.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------
-- Tabela de Usuários (necessária para a FK em 'orders')
-- Simplificada para o escopo atual do projeto.
-- -----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.users (
                                            "Id" UUID PRIMARY KEY,
                                            "Username" VARCHAR(100) NOT NULL UNIQUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                              );

COMMENT ON TABLE public.users IS 'Armazena os usuários do sistema. Necessário para relacionar os pedidos.';


-- -----------------------------------------------------------------
-- Tabela de Pedidos (a principal entidade do nosso fluxo)
-- A estrutura reflete a entidade de domínio `Order`. Os nomes das
-- colunas com aspas correspondem exatamente ao que o Dapper espera.
-- -----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.orders (
                                             "Id" UUID PRIMARY KEY,
                                             "UserId" UUID NOT NULL REFERENCES public.users("Id"),
    "TotalAmount" DECIMAL(18, 2) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL
                              );

COMMENT ON TABLE public.orders IS 'Armazena os pedidos criados na aplicação.';
CREATE INDEX IF NOT EXISTS idx_orders_user_id ON public.orders("UserId");


-- -----------------------------------------------------------------
-- Dados Iniciais
-- Insere um usuário de exemplo para que seja possível criar pedidos
-- associados a ele via API. O UUID é fixo para facilitar os testes.
-- -----------------------------------------------------------------
-- O ON CONFLICT evita erros caso o script seja executado múltiplas vezes.
INSERT INTO public.users ("Id", "Username") VALUES
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'testuser')
    ON CONFLICT ("Id") DO NOTHING;