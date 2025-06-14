-- Criar tabela de exemplo para demonstração de traces
CREATE TABLE IF NOT EXISTS weather_data (
    id SERIAL PRIMARY KEY,
    date DATE NOT NULL,
    temperature_c INTEGER NOT NULL,
    summary VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Índice para otimizar consultas por data
CREATE INDEX IF NOT EXISTS idx_weather_data_date ON weather_data(date);

-- Função para atualizar o timestamp
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
-- =================================================================
-- SCRIPT DE INICIALIZAÇÃO DO POSTGRESQL
-- =================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Tabela de usuários para exemplo
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    full_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de produtos para exemplo
CREATE TABLE IF NOT EXISTS products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    price DECIMAL(10, 2) NOT NULL,
    stock INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de pedidos para exemplo
CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id),
    total_amount DECIMAL(10, 2) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de itens de pedido para exemplo
CREATE TABLE IF NOT EXISTS order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id),
    product_id UUID NOT NULL REFERENCES products(id),
    quantity INTEGER NOT NULL,
    unit_price DECIMAL(10, 2) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Dados de exemplo para usuários
INSERT INTO users (username, email, full_name) VALUES
    ('johndoe', 'john.doe@example.com', 'John Doe'),
    ('janedoe', 'jane.doe@example.com', 'Jane Doe'),
    ('bobsmith', 'bob.smith@example.com', 'Bob Smith');

-- Dados de exemplo para produtos
INSERT INTO products (name, description, price, stock) VALUES
    ('Smartphone', 'High-end smartphone with great camera', 999.99, 50),
    ('Laptop', 'Powerful laptop for professionals', 1499.99, 30),
    ('Headphones', 'Wireless noise-cancelling headphones', 299.99, 100),
    ('Smartwatch', 'Fitness tracking smartwatch', 199.99, 75),
    ('Tablet', '10-inch tablet with high resolution display', 399.99, 45);

-- Função para atualizar o timestamp de updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE 'plpgsql';

-- Triggers para atualizar updated_at
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_products_updated_at BEFORE UPDATE ON products FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_orders_updated_at BEFORE UPDATE ON orders FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_order_items_updated_at BEFORE UPDATE ON order_items FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
-- Trigger para atualizar o timestamp
CREATE TRIGGER weather_data_updated_at
BEFORE UPDATE ON weather_data
FOR EACH ROW
EXECUTE FUNCTION update_updated_at();

-- Inserir alguns dados iniciais
INSERT INTO weather_data (date, temperature_c, summary)
VALUES
    (CURRENT_DATE, 22, 'Mild'),
    (CURRENT_DATE + INTERVAL '1 day', 25, 'Warm'),
    (CURRENT_DATE + INTERVAL '2 days', 19, 'Cool'),
    (CURRENT_DATE + INTERVAL '3 days', 15, 'Chilly'),
    (CURRENT_DATE + INTERVAL '4 days', 28, 'Hot')
ON CONFLICT DO NOTHING;

-- Comentários para documentação
COMMENT ON TABLE weather_data IS 'Tabela para armazenar dados de previsão do tempo';
COMMENT ON COLUMN weather_data.id IS 'Identificador único';
COMMENT ON COLUMN weather_data.date IS 'Data da previsão';
COMMENT ON COLUMN weather_data.temperature_c IS 'Temperatura em graus Celsius';
COMMENT ON COLUMN weather_data.summary IS 'Descrição resumida do clima';
COMMENT ON COLUMN weather_data.created_at IS 'Data e hora de criação do registro';
COMMENT ON COLUMN weather_data.updated_at IS 'Data e hora da última atualização';
